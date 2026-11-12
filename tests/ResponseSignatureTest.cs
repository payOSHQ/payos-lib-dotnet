using System.Net;
using System.Text;

using PayOS.Exceptions;

using Xunit;

namespace PayOS.Tests;

public class ResponseSignatureTest
{
    private sealed class UnsignedResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"code\":\"00\",\"desc\":\"success\",\"data\":{\"id\":\"x\"}}",
                    Encoding.UTF8,
                    "application/json")
            });
        }
    }

    private static PayOSClient CreateClient() =>
        new(new PayOSOptions
        {
            ClientId = "c",
            ApiKey = "a",
            ChecksumKey = "k",
            MaxRetries = 0,
            HttpClient = new HttpClient(new UnsignedResponseHandler())
        });

    [Fact]
    public async Task BodySignedResponseWithoutSignatureThrows()
    {
        var ex = await Assert.ThrowsAsync<InvalidSignatureException>(() => CreateClient().PaymentRequests.GetAsync("x"));
        Assert.Equal("Missing response signature", ex.Message);
    }

    [Fact]
    public async Task HeaderSignedResponseWithoutSignatureThrows()
    {
        var ex = await Assert.ThrowsAsync<InvalidSignatureException>(() => CreateClient().Payouts.GetAsync("x"));
        Assert.Equal("Missing response signature", ex.Message);
    }
}
