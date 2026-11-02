using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using PayOS.Exceptions;

namespace PayOS.Internal;

/// <summary>
/// Follows redirects manually so that the merchant credentials (x-api-key, x-client-id, x-partner-code,
/// authorization, cookie, proxy-authorization) are never forwarded to another origin. The platform handlers
/// only drop <c>Authorization</c> on redirects, so the custom payOS headers would otherwise be sent to the new host.
/// The inner handler must have automatic redirects disabled.
/// </summary>
internal sealed class SafeRedirectHandler : DelegatingHandler
{
    internal const int MaxRedirects = 5;

    private static readonly string[] SensitiveHeaders =
    {
        "x-api-key", "x-client-id", "x-partner-code", "authorization", "cookie", "proxy-authorization"
    };

    public SafeRedirectHandler(HttpMessageHandler inner) : base(inner)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Buffer the body once so it can be replayed on 307/308 redirects.
        byte[]? body = null;
        if (request.Content != null)
        {
            body = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        for (var hop = 0; ; hop++)
        {
            var status = (int)response.StatusCode;
            var location = response.Headers.Location;
            if (status < 300 || status >= 400 || location == null || request.RequestUri == null)
            {
                return response;
            }
            if (hop >= MaxRedirects)
            {
                response.Dispose();
                throw new PayOSException($"Too many redirects (more than {MaxRedirects})");
            }

            var current = request.RequestUri;
            var target = location.IsAbsoluteUri ? location : new Uri(current, location);
            var switchToGet = status == 303 || ((status == 301 || status == 302) && request.Method == HttpMethod.Post);
            var sameOrigin = string.Equals(current.Scheme, target.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(current.Host, target.Host, StringComparison.OrdinalIgnoreCase)
                && current.Port == target.Port;

            var next = new HttpRequestMessage(switchToGet ? HttpMethod.Get : request.Method, target) { Version = request.Version };
            foreach (var header in request.Headers)
            {
                if (!sameOrigin && Array.Exists(SensitiveHeaders, h => string.Equals(h, header.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                next.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (switchToGet)
            {
                body = null;
            }
            else if (body != null && request.Content != null)
            {
                var content = new ByteArrayContent(body);
                foreach (var header in request.Content.Headers)
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                next.Content = content;
            }

            response.Dispose();
            if (hop > 0)
            {
                // Intermediate requests are owned here; the original one is owned by the caller.
                request.Dispose();
            }
            request = next;
            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}