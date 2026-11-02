using System.Net;
using System.Text;

using PayOS.Exceptions;

using Xunit;

namespace PayOS.Tests;

public class PathAndResponseDataTest
{
    private sealed class RecordingHandler(string body) : HttpMessageHandler
    {
        public List<Uri> Uris { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uris.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private static PayOSClient CreateClient(HttpMessageHandler handler) =>
        new(new PayOSOptions
        {
            ClientId = "c",
            ApiKey = "a",
            ChecksumKey = "k",
            MaxRetries = 0,
            HttpClient = new HttpClient(handler)
        });

    private static RecordingHandler ResponseWithData(string data) =>
        new($"{{\"code\":\"00\",\"desc\":\"success\",\"data\":{data},\"signature\":\"00\"}}");

    [Fact]
    public async Task PathSegmentsArePercentEncoded()
    {
        var handler = ResponseWithData("{\"id\":\"x\"}");
        await Assert.ThrowsAnyAsync<PayOSException>(() => CreateClient(handler).PaymentRequests.GetAsync("../../v1/payouts-account/balance"));
        Assert.Equal("/v2/payment-requests/..%2F..%2Fv1%2Fpayouts-account%2Fbalance", handler.Uris.Single().AbsolutePath);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(9007199254740992L)]
    public async Task UnsafeOrderCodesAreRejectedBeforeSending(long orderCode)
    {
        var handler = ResponseWithData("{\"id\":\"x\"}");
        var client = CreateClient(handler);
        await Assert.ThrowsAsync<PayOSException>(() => client.PaymentRequests.GetAsync(orderCode));
        await Assert.ThrowsAsync<PayOSException>(() => client.PaymentRequests.CancelAsync(orderCode));
        await Assert.ThrowsAsync<PayOSException>(() => client.PaymentRequests.Invoices.GetAsync(orderCode));
        await Assert.ThrowsAsync<PayOSException>(() => client.PaymentRequests.Invoices.DownloadAsync("i", orderCode));
        Assert.Empty(handler.Uris);
    }

    [Theory]
    [InlineData("{\"id\":\"x&amount=1\"}")]
    [InlineData("{\"extra\":{\"a\":1}}")]
    [InlineData("{\"transactions\":[{\"a\":{\"b\":1}}]}")]
    [InlineData("{\"amount\":-9223372036854775808}")]
    public async Task UnsafeBodySignedDataIsRejected(string data)
    {
        var ex = await Assert.ThrowsAsync<InvalidSignatureException>(() => CreateClient(ResponseWithData(data)).PaymentRequests.GetAsync("x"));
        Assert.StartsWith("Invalid response data", ex.Message);
    }
}