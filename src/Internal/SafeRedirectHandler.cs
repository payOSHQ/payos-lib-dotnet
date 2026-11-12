using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using PayOS.Exceptions;

namespace PayOS.Internal;

/// <summary>
/// Follows redirects manually so that the merchant credentials (x-api-key, x-client-id, x-partner-code,
/// authorization, cookie, proxy-authorization) are never forwarded once the chain leaves the original origin.
/// The platform handlers only drop <c>Authorization</c> on redirects, so the custom payOS headers would otherwise
/// be sent to the new host. Cookies are handled here as well, because a handler-level cookie container would add
/// them on every hop; the inner handler must have automatic redirects and cookies disabled.
/// </summary>
internal sealed class SafeRedirectHandler : DelegatingHandler
{
    internal const int MaxRedirects = 5;

    private static readonly string[] SensitiveHeaders =
    {
        "x-api-key", "x-client-id", "x-partner-code", "authorization", "cookie", "proxy-authorization"
    };

    private readonly CookieContainer _cookies = new();

    public SafeRedirectHandler(HttpMessageHandler inner) : base(inner)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var origin = request.RequestUri;
        if (origin == null || !origin.IsAbsoluteUri)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // Buffer the body once so it can be replayed on 307/308 redirects.
        byte[]? body = null;
        if (request.Content != null)
        {
            body = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        // Snapshot before jar cookies are added so they are not copied into the next hop twice.
        var headers = request.Headers.ToList();
        var leftOrigin = false;

        var response = await SendWithCookiesAsync(request, origin, cancellationToken).ConfigureAwait(false);

        for (var hop = 0; ; hop++)
        {
            var location = response.Headers.Location;
            if (!IsFollowedRedirect(response.StatusCode) || location == null)
            {
                return response;
            }
            if (hop >= MaxRedirects)
            {
                response.Dispose();
                throw new PayOSException($"Too many redirects (more than {MaxRedirects})");
            }

            var status = (int)response.StatusCode;
            var target = location.IsAbsoluteUri ? location : new Uri(request.RequestUri!, location);
            var switchToGet = status == 303 || ((status == 301 || status == 302) && request.Method == HttpMethod.Post);
            leftOrigin = leftOrigin || !SameOrigin(origin, target);

            var next = new HttpRequestMessage(switchToGet ? HttpMethod.Get : request.Method, target) { Version = request.Version };
            foreach (var header in headers)
            {
                if (leftOrigin && Array.Exists(SensitiveHeaders, h => string.Equals(h, header.Key, StringComparison.OrdinalIgnoreCase)))
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
            response = leftOrigin
                ? await SendAndStoreCookiesAsync(request, target, cancellationToken).ConfigureAwait(false)
                : await SendWithCookiesAsync(request, target, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsFollowedRedirect(HttpStatusCode status) =>
        (int)status is 301 or 302 or 303 or 307 or 308;

    // Uri.Port already resolves the scheme's default port, so ":443" and no port compare equal.
    private static bool SameOrigin(Uri a, Uri b) =>
        string.Equals(a.Scheme, b.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase)
        && a.Port == b.Port;

    private Task<HttpResponseMessage> SendWithCookiesAsync(HttpRequestMessage request, Uri uri, CancellationToken cancellationToken)
    {
        var jarCookies = _cookies.GetCookieHeader(uri);
        if (jarCookies.Length > 0)
        {
            if (request.Headers.TryGetValues("Cookie", out var explicitCookies))
            {
                jarCookies = string.Join("; ", explicitCookies.Append(jarCookies));
                request.Headers.Remove("Cookie");
            }
            request.Headers.TryAddWithoutValidation("Cookie", jarCookies);
        }
        return SendAndStoreCookiesAsync(request, uri, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAndStoreCookiesAsync(HttpRequestMessage request, Uri uri, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var setCookie in setCookies)
            {
                try
                {
                    _cookies.SetCookies(uri, setCookie);
                }
                catch (CookieException)
                {
                    // Invalid cookies are ignored, as the platform handler does.
                }
            }
        }
        return response;
    }
}