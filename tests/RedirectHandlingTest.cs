using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

using PayOS.Crypto;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;

using Xunit;

namespace PayOS.Tests;

/// <summary>
/// Redirect behaviour of the default HttpClient created by <see cref="PayOSClient"/>, exercised against two local
/// HTTP listeners on different ports (each also reachable as both 127.0.0.1 and localhost).
/// </summary>
public sealed class RedirectHandlingTest : IDisposable
{
    private const string ChecksumKey = "checksum-key";
    private const string PaymentLinkId = "0f5c7a1e9b2d4c3a8e6f1b2c3d4e5f60";
    private const string StartPath = "/v2/payment-requests/" + PaymentLinkId;
    private const string CancelPath = StartPath + "/cancel";

    private static readonly string[] CredentialHeaders =
    {
        "x-api-key", "x-client-id", "x-partner-code", "authorization", "cookie", "proxy-authorization"
    };

    private readonly TestServer _origin = new();
    private readonly TestServer _other = new();

    public void Dispose()
    {
        _origin.Dispose();
        _other.Dispose();
    }

    private sealed record Reply(int Status, string? Location = null, string Body = "");

    private sealed record RecordedRequest(string Method, string Host, string Path, Dictionary<string, string> Headers, string Body);

    private sealed class TestServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly ConcurrentDictionary<string, Reply> _routes = new();

        public TestServer()
        {
            Port = FreePort();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();
            _ = Task.Run(ServeAsync);
        }

        public int Port { get; }

        public ConcurrentQueue<RecordedRequest> Requests { get; } = new();

        public string Url(string path, string host = "127.0.0.1") => $"http://{host}:{Port}{path}";

        public void Route(string path, Reply reply) => _routes[path] = reply;

        public void Dispose() => _listener.Close();

        private static int FreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        private async Task ServeAsync()
        {
            while (true)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }

                var request = context.Request;
                using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
                var body = await reader.ReadToEndAsync();
                var headers = request.Headers.AllKeys
                    .Where(k => k != null)
                    .ToDictionary(k => k!.ToLowerInvariant(), k => request.Headers[k]!);
                var path = request.Url!.AbsolutePath;
                Requests.Enqueue(new RecordedRequest(request.HttpMethod, request.Url.Host, path, headers, body));

                var reply = _routes.TryGetValue(path, out var r) ? r : new Reply(404);
                var response = context.Response;
                response.StatusCode = reply.Status;
                if (reply.Location != null)
                {
                    response.RedirectLocation = reply.Location;
                }
                var bytes = Encoding.UTF8.GetBytes(reply.Body);
                response.ContentType = "application/json";
                response.ContentLength64 = bytes.Length;
                await response.OutputStream.WriteAsync(bytes);
                response.Close();
            }
        }
    }

    private static readonly string SignedPaymentLinkBody = BuildSignedPaymentLinkBody();

    private static string BuildSignedPaymentLinkBody()
    {
        var data = "{\"id\":\"" + PaymentLinkId + "\",\"orderCode\":1730540000123,\"amount\":10000,\"amountPaid\":0," +
                   "\"amountRemaining\":10000,\"status\":\"PENDING\",\"createdAt\":\"2026-11-02T09:15:00+07:00\"," +
                   "\"transactions\":[],\"cancellationReason\":null,\"canceledAt\":null}";
        var signature = new CryptoProvider().CreateSignatureFromObject(JsonSerializer.Deserialize<PaymentLink>(data)!, ChecksumKey);
        return $"{{\"code\":\"00\",\"desc\":\"success\",\"data\":{data},\"signature\":\"{signature}\"}}";
    }

    private PayOSClient CreateClient(HttpClient? httpClient = null) =>
        new(new PayOSOptions
        {
            ClientId = "client-id",
            ApiKey = "api-key",
            ChecksumKey = ChecksumKey,
            PartnerCode = "partner-code",
            BaseUrl = _origin.Url(string.Empty),
            MaxRetries = 0,
            LogLevel = Microsoft.Extensions.Logging.LogLevel.None,
            HttpClient = httpClient,
            DefaultHeaders = new Dictionary<string, string>
            {
                ["authorization"] = "Bearer token",
                ["cookie"] = "session=abc",
                ["proxy-authorization"] = "Basic cHJveHk6cHJveHk="
            }
        });

    private static void AssertHasCredentials(RecordedRequest request)
    {
        Assert.Equal("client-id", request.Headers["x-client-id"]);
        Assert.Equal("api-key", request.Headers["x-api-key"]);
        Assert.Equal("partner-code", request.Headers["x-partner-code"]);
        Assert.Equal("Bearer token", request.Headers["authorization"]);
        Assert.Contains("session=abc", request.Headers["cookie"]);
        Assert.Equal("Basic cHJveHk6cHJveHk=", request.Headers["proxy-authorization"]);
    }

    private static void AssertHasNoCredentials(RecordedRequest request)
    {
        foreach (var header in CredentialHeaders)
        {
            Assert.False(request.Headers.ContainsKey(header), $"{header} was sent to {request.Host}{request.Path}");
        }
    }

    [Fact]
    public async Task CrossOriginRedirectToOtherPortRemovesCredentialHeaders()
    {
        _origin.Route(StartPath, new Reply(302, _other.Url("/final")));
        _other.Route("/final", new Reply(200, Body: SignedPaymentLinkBody));

        var link = await CreateClient().PaymentRequests.GetAsync(PaymentLinkId);

        Assert.Equal(PaymentLinkId, link.Id);
        AssertHasCredentials(Assert.Single(_origin.Requests));
        var redirected = Assert.Single(_other.Requests);
        AssertHasNoCredentials(redirected);
        Assert.StartsWith("PayOS.Net/", redirected.Headers["user-agent"]);
    }

    [Fact]
    public async Task CrossOriginRedirectToOtherHostRemovesCredentialHeaders()
    {
        _origin.Route(StartPath, new Reply(302, _origin.Url("/final", host: "localhost")));
        _origin.Route("/final", new Reply(200, Body: SignedPaymentLinkBody));

        await CreateClient().PaymentRequests.GetAsync(PaymentLinkId);

        var requests = _origin.Requests.ToArray();
        Assert.Equal(2, requests.Length);
        Assert.Equal("127.0.0.1", requests[0].Host);
        AssertHasCredentials(requests[0]);
        Assert.Equal("localhost", requests[1].Host);
        AssertHasNoCredentials(requests[1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SameOriginRedirectKeepsCredentialHeaders(bool relativeLocation)
    {
        _origin.Route(StartPath, new Reply(302, relativeLocation ? "/final" : _origin.Url("/final")));
        _origin.Route("/final", new Reply(200, Body: SignedPaymentLinkBody));

        await CreateClient().PaymentRequests.GetAsync(PaymentLinkId);

        var requests = _origin.Requests.ToArray();
        Assert.Equal(new[] { StartPath, "/final" }, requests.Select(r => r.Path));
        Assert.All(requests, AssertHasCredentials);
    }

    [Fact]
    public async Task CredentialHeadersStayRemovedAfterReturningToOriginalOrigin()
    {
        _origin.Route(StartPath, new Reply(302, _other.Url("/hop")));
        _other.Route("/hop", new Reply(302, _origin.Url("/final")));
        _origin.Route("/final", new Reply(200, Body: SignedPaymentLinkBody));

        await CreateClient().PaymentRequests.GetAsync(PaymentLinkId);

        var originRequests = _origin.Requests.ToArray();
        Assert.Equal(new[] { StartPath, "/final" }, originRequests.Select(r => r.Path));
        AssertHasCredentials(originRequests[0]);
        AssertHasNoCredentials(Assert.Single(_other.Requests));
        AssertHasNoCredentials(originRequests[1]);
    }

    [Theory]
    [InlineData(301, "GET")]
    [InlineData(302, "GET")]
    [InlineData(303, "GET")]
    [InlineData(307, "POST")]
    [InlineData(308, "POST")]
    public async Task PostRedirectMethodAndBodyFollowStatusCode(int status, string expectedMethod)
    {
        _origin.Route(CancelPath, new Reply(status, "/final"));
        _origin.Route("/final", new Reply(200, Body: SignedPaymentLinkBody));

        await CreateClient().PaymentRequests.CancelAsync(PaymentLinkId, "customer request");

        var requests = _origin.Requests.ToArray();
        Assert.Equal(2, requests.Length);
        Assert.Equal("POST", requests[0].Method);
        var followed = requests[1];
        Assert.Equal(expectedMethod, followed.Method);
        if (expectedMethod == "POST")
        {
            Assert.Equal(requests[0].Body, followed.Body);
            Assert.Contains("customer request", followed.Body);
            Assert.StartsWith("application/json", followed.Headers["content-type"]);
        }
        else
        {
            Assert.Equal(string.Empty, followed.Body);
            Assert.False(followed.Headers.ContainsKey("content-type"));
            Assert.False(followed.Headers.TryGetValue("content-length", out var length) && length != "0");
        }
    }

    [Theory]
    [InlineData(301)]
    [InlineData(302)]
    [InlineData(303)]
    [InlineData(307)]
    [InlineData(308)]
    public async Task GetRedirectStaysGet(int status)
    {
        _origin.Route(StartPath, new Reply(status, "/final"));
        _origin.Route("/final", new Reply(200, Body: SignedPaymentLinkBody));

        await CreateClient().PaymentRequests.GetAsync(PaymentLinkId);

        Assert.All(_origin.Requests, r => Assert.Equal("GET", r.Method));
    }

    private void RouteRedirectChain(int redirects)
    {
        var path = StartPath;
        for (var i = 1; i <= redirects; i++)
        {
            var next = $"/hop{i}";
            _origin.Route(path, new Reply(302, next));
            path = next;
        }
        _origin.Route(path, new Reply(200, Body: SignedPaymentLinkBody));
    }

    [Fact]
    public async Task FiveRedirectsAreFollowed()
    {
        RouteRedirectChain(5);

        var link = await CreateClient().PaymentRequests.GetAsync(PaymentLinkId);

        Assert.Equal(PaymentLinkId, link.Id);
        Assert.Equal(6, _origin.Requests.Count);
    }

    [Fact]
    public async Task MoreThanFiveRedirectsFail()
    {
        RouteRedirectChain(6);

        var ex = await Assert.ThrowsAsync<PayOSException>(() => CreateClient().PaymentRequests.GetAsync(PaymentLinkId));

        Assert.Equal("Too many redirects (more than 5)", ex.Message);
        Assert.Equal(6, _origin.Requests.Count);
    }

    [Fact]
    public async Task CallerHttpClientWithoutAutoRedirectReturnsRedirectUnchanged()
    {
        _origin.Route(StartPath, new Reply(302, _other.Url("/final")));
        _other.Route("/final", new Reply(200, Body: SignedPaymentLinkBody));
        using var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        var ex = await Assert.ThrowsAsync<ApiException>(() => CreateClient(httpClient).PaymentRequests.GetAsync(PaymentLinkId));

        Assert.Equal(302, ex.StatusCode);
        AssertHasCredentials(Assert.Single(_origin.Requests));
        Assert.Empty(_other.Requests);
    }

    [Fact]
    public async Task CallerHttpClientFollowsItsOwnRedirectPolicy()
    {
        // Documented on PayOSOptions.HttpClient: a caller-supplied client is used as-is, so its own handler decides
        // which headers reach the redirect target.
        _origin.Route(StartPath, new Reply(302, _other.Url("/final")));
        _other.Route("/final", new Reply(200, Body: SignedPaymentLinkBody));
        using var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });

        await CreateClient(httpClient).PaymentRequests.GetAsync(PaymentLinkId);

        var redirected = Assert.Single(_other.Requests);
        Assert.Equal("api-key", redirected.Headers["x-api-key"]);
        Assert.Equal("client-id", redirected.Headers["x-client-id"]);
    }
}