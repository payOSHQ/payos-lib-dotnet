using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PayOS.Internal;

/// <summary>
/// Follows redirects manually so that the payOS credential headers (x-api-key, x-client-id, x-partner-code)
/// are never forwarded to another origin. The platform handlers only drop <c>Authorization</c> on cross-host
/// redirects, so these custom headers would otherwise be sent to the new host.
/// The inner handler must have automatic redirects disabled.
/// </summary>
internal sealed class SafeRedirectHandler : DelegatingHandler
{
    private const int MaxRedirects = 10;
    private static readonly string[] SensitiveHeaders = { "x-api-key", "x-client-id", "x-partner-code" };

    public SafeRedirectHandler(HttpMessageHandler inner) : base(inner)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var origin = request.RequestUri;
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        for (var hop = 0; hop < MaxRedirects; hop++)
        {
            var location = response.Headers.Location;
            if (!IsRedirect(response.StatusCode) || location == null || origin == null)
            {
                return response;
            }

            var target = location.IsAbsoluteUri ? location : new Uri(request.RequestUri!, location);
            var method = request.Method;
            var content = request.Content;
            if (response.StatusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther)
            {
                if (method != HttpMethod.Head)
                {
                    method = HttpMethod.Get;
                }
                content = null;
            }
            else if (content != null)
            {
                // 307/308 with a body: it cannot be replayed safely; hand the redirect response to the caller.
                return response;
            }

            var next = new HttpRequestMessage(method, target) { Version = request.Version, Content = content };
            var sameOrigin = string.Equals(origin.Scheme, target.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(origin.Host, target.Host, StringComparison.OrdinalIgnoreCase)
                && origin.Port == target.Port;
            foreach (var header in request.Headers)
            {
                if (!sameOrigin && Array.Exists(SensitiveHeaders, h => string.Equals(h, header.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                next.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            response.Dispose();
            request = next;
            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    private static bool IsRedirect(HttpStatusCode code) =>
        code == HttpStatusCode.MovedPermanently || code == HttpStatusCode.Found || code == HttpStatusCode.SeeOther
        || code == HttpStatusCode.TemporaryRedirect || (int)code == 308;
}
