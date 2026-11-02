using System.Net;
using System.Text;
using System.Text.Json;

using PayOS.Crypto;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.V2.PaymentRequests.Invoices;

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

    private const string ChecksumKey = "k";

    private static async Task Settle(Task task)
    {
        try
        {
            await task;
        }
        catch (PayOSException)
        {
            // Only the outgoing request URL matters here.
        }
    }

    private static async Task AssertRejected(Func<Task> call, string message)
    {
        var ex = await Assert.ThrowsAsync<PayOSException>(call);
        Assert.Equal(message, ex.Message);
    }

    public static TheoryData<string, string> EncodedIds => new()
    {
        { "pl_0f5c7a1e9b2d", "pl_0f5c7a1e9b2d" },
        { "../x", "..%2Fx" },
        { "a/b", "a%2Fb" },
        { "a?b", "a%3Fb" },
        { "a#b", "a%23b" },
        { "a%2Fb", "a%252Fb" },
    };

    [Theory]
    [MemberData(nameof(EncodedIds))]
    public async Task IdsAreSentAsSinglePathSegment(string id, string segment)
    {
        var handler = ResponseWithData("{\"id\":\"x\"}");
        var client = CreateClient(handler);

        await Settle(client.PaymentRequests.GetAsync(id));
        await Settle(client.PaymentRequests.CancelAsync(id, "reason"));
        await Settle(client.PaymentRequests.Invoices.GetAsync(id));
        await Settle(client.PaymentRequests.Invoices.DownloadAsync("inv_1", id));
        await Settle(client.PaymentRequests.Invoices.DownloadAsync(id, "pl_1"));
        await Settle(client.Payouts.GetAsync(id));

        Assert.Equal(
            new[]
            {
                $"/v2/payment-requests/{segment}",
                $"/v2/payment-requests/{segment}/cancel",
                $"/v2/payment-requests/{segment}/invoices",
                $"/v2/payment-requests/{segment}/invoices/inv_1/download",
                $"/v2/payment-requests/pl_1/invoices/{segment}/download",
                $"/v1/payouts/{segment}",
            },
            handler.Uris.Select(u => u.AbsolutePath));
        Assert.All(handler.Uris, u =>
        {
            Assert.Equal(string.Empty, u.Query);
            Assert.Equal(string.Empty, u.Fragment);
        });
    }

    [Theory]
    [InlineData("", "must be a non-empty string")]
    [InlineData(".", "must not be a dot segment")]
    [InlineData("..", "must not be a dot segment")]
    public async Task EmptyAndDotSegmentIdsAreRejectedBeforeSending(string id, string reason)
    {
        var handler = ResponseWithData("{\"id\":\"x\"}");
        var client = CreateClient(handler);

        await AssertRejected(() => client.PaymentRequests.GetAsync(id), $"idOrOrderCode {reason}");
        await AssertRejected(() => client.PaymentRequests.CancelAsync(id, "reason"), $"idOrOrderCode {reason}");
        await AssertRejected(() => client.PaymentRequests.Invoices.GetAsync(id), $"paymentLinkId {reason}");
        await AssertRejected(() => client.PaymentRequests.Invoices.DownloadAsync("inv_1", id), $"paymentLinkId {reason}");
        await AssertRejected(() => client.PaymentRequests.Invoices.DownloadAsync(id, "pl_1"), $"invoiceId {reason}");
        await AssertRejected(() => client.Payouts.GetAsync(id), $"payoutId {reason}");
        Assert.Empty(handler.Uris);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(9007199254740992L)]
    public async Task UnsafeOrderCodesAreRejectedWithMessage(long orderCode)
    {
        var handler = ResponseWithData("{\"id\":\"x\"}");
        var client = CreateClient(handler);
        const string message = "orderCode must be a non-negative safe integer";

        await AssertRejected(() => client.PaymentRequests.GetAsync(orderCode), message);
        await AssertRejected(() => client.PaymentRequests.CancelAsync(orderCode), message);
        await AssertRejected(() => client.PaymentRequests.Invoices.GetAsync(orderCode), message);
        await AssertRejected(() => client.PaymentRequests.Invoices.DownloadAsync("inv_1", orderCode), message);
        Assert.Empty(handler.Uris);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1730540000123L)]
    [InlineData(9007199254740991L)]
    public async Task SafeOrderCodesAreSentAsDecimalSegment(long orderCode)
    {
        var handler = ResponseWithData("{\"id\":\"x\"}");
        var client = CreateClient(handler);

        await Settle(client.PaymentRequests.GetAsync(orderCode));
        await Settle(client.PaymentRequests.CancelAsync(orderCode));
        await Settle(client.PaymentRequests.Invoices.GetAsync(orderCode));
        await Settle(client.PaymentRequests.Invoices.DownloadAsync("inv_1", orderCode));

        Assert.Equal(
            new[]
            {
                $"/v2/payment-requests/{orderCode}",
                $"/v2/payment-requests/{orderCode}/cancel",
                $"/v2/payment-requests/{orderCode}/invoices",
                $"/v2/payment-requests/{orderCode}/invoices/inv_1/download",
            },
            handler.Uris.Select(u => u.AbsolutePath));
    }

    /// <summary>Response whose body signature is computed with the SDK's own helper over the deserialised data.</summary>
    private static RecordingHandler SignedResponse<T>(string data)
    {
        var model = JsonSerializer.Deserialize<T>(data)!;
        var signature = new CryptoProvider().CreateSignatureFromObject(model, ChecksumKey);
        return new($"{{\"code\":\"00\",\"desc\":\"success\",\"data\":{data},\"signature\":\"{signature}\"}}");
    }

    private static string PaymentLinkData(
        string id = "0f5c7a1e9b2d4c3a8e6f1b2c3d4e5f60",
        string amount = "10000",
        string description = "CSKJ2X1 Thanh toan don hang 1730540000123",
        string extraTransactionField = "",
        string extraField = "") =>
        "{\"id\":\"" + id + "\",\"orderCode\":1730540000123,\"amount\":" + amount +
        ",\"amountPaid\":10000,\"amountRemaining\":0,\"status\":\"PAID\",\"createdAt\":\"2026-11-02T09:15:00+07:00\"," +
        "\"transactions\":[{\"reference\":\"FT26306123456789\",\"amount\":10000,\"accountNumber\":\"0123456789\"," +
        "\"description\":\"" + description + "\",\"transactionDateTime\":\"2026-11-02 09:16:12\"," +
        "\"virtualAccountName\":null,\"virtualAccountNumber\":null,\"counterAccountBankId\":\"970422\"," +
        "\"counterAccountBankName\":\"MB Bank\",\"counterAccountName\":\"NGUYEN VAN A\",\"counterAccountNumber\":\"9876543210\"" +
        extraTransactionField + "}],\"cancellationReason\":null,\"canceledAt\":null" + extraField + "}";

    private const string InvoicesData =
        "{\"invoices\":[{\"invoiceId\":\"inv_7d1e2f3a\",\"invoiceNumber\":\"0000123\",\"issuedTimestamp\":1730540172," +
        "\"issuedDatetime\":\"2026-11-02T09:16:12\",\"transactionId\":\"FT26306123456789\",\"reservationCode\":\"R6K2Q9\"," +
        "\"codeOfTax\":\"M1-26-ABCDE-00000000123\"}]}";

    [Fact]
    public async Task SignedPaymentLinkResponseIsAccepted()
    {
        var client = CreateClient(SignedResponse<PaymentLink>(PaymentLinkData()));

        var link = await client.PaymentRequests.GetAsync("0f5c7a1e9b2d4c3a8e6f1b2c3d4e5f60");

        Assert.Equal(1730540000123, link.OrderCode);
        Assert.Equal(PaymentLinkStatus.Paid, link.Status);
        Assert.Equal("FT26306123456789", Assert.Single(link.Transactions!).Reference);
    }

    [Fact]
    public async Task SignedCancelResponseIsAccepted()
    {
        var client = CreateClient(SignedResponse<PaymentLink>(PaymentLinkData()));

        var link = await client.PaymentRequests.CancelAsync(1730540000123, "customer request");

        Assert.Equal("0f5c7a1e9b2d4c3a8e6f1b2c3d4e5f60", link.Id);
    }

    [Fact]
    public async Task SignedInvoicesResponseIsAccepted()
    {
        var client = CreateClient(SignedResponse<InvoicesInfo>(InvoicesData));

        var info = await client.PaymentRequests.Invoices.GetAsync("0f5c7a1e9b2d4c3a8e6f1b2c3d4e5f60");

        Assert.Equal("inv_7d1e2f3a", Assert.Single(info.Invoices).InvoiceId);
    }

    public static TheoryData<string, string> SignedButRejectedPaymentLinkData => new()
    {
        { PaymentLinkData(id: "pl_1&amount=1"), "\"id\" must not contain a \"&key=\" sequence" },
        { PaymentLinkData(description: "don hang&reference=FT1"), "\"transactions[0].description\" must not contain a \"&key=\" sequence" },
        { PaymentLinkData(extraField: ",\"bad-key\":1"), "unsupported key \"bad-key\"" },
        { PaymentLinkData(extraField: ",\"__proto__\":1"), "unsupported key \"__proto__\"" },
        { PaymentLinkData(extraTransactionField: ",\"a b\":1"), "unsupported key \"transactions[0].a b\"" },
        { PaymentLinkData(extraField: ",\"extra\\n\":1"), "unsupported key \"extra\n\"" },
        { PaymentLinkData(extraTransactionField: ",\"memo\\n\":1"), "unsupported key \"transactions[0].memo\n\"" },
        { PaymentLinkData(extraField: ",\"extra\":{\"a\":1}"), "\"extra\" has an unsupported type" },
        { PaymentLinkData(extraField: ",\"extra\":[[1]]"), "\"extra[0]\" has an unsupported type" },
        { PaymentLinkData(extraTransactionField: ",\"meta\":{\"a\":1}"), "\"transactions[0].meta\" has an unsupported type" },
        { PaymentLinkData(extraField: ",\"extra\":1e400"), "\"extra\" must be a finite number within the safe integer range" },
        { PaymentLinkData(extraField: ",\"extra\":1e16"), "\"extra\" must be a finite number within the safe integer range" },
        { PaymentLinkData(amount: "9007199254740993"), "\"amount\" must be a safe integer" },
        { PaymentLinkData(amount: "-9007199254740993"), "\"amount\" must be a safe integer" },
    };

    [Theory]
    [MemberData(nameof(SignedButRejectedPaymentLinkData))]
    public async Task CorrectlySignedButUnsupportedPaymentLinkDataIsRejected(string data, string reason)
    {
        var client = CreateClient(SignedResponse<PaymentLink>(data));

        var ex = await Assert.ThrowsAsync<InvalidSignatureException>(() => client.PaymentRequests.GetAsync("0f5c7a1e9b2d4c3a8e6f1b2c3d4e5f60"));
        Assert.Equal($"Invalid response data: {reason}", ex.Message);
    }

    [Fact]
    public async Task CorrectlySignedInvoiceDataWithAmpersandKeySequenceIsRejected()
    {
        var client = CreateClient(SignedResponse<InvoicesInfo>(InvoicesData.Replace("0000123", "0000123&invoiceId=x")));

        var ex = await Assert.ThrowsAsync<InvalidSignatureException>(() => client.PaymentRequests.Invoices.GetAsync("0f5c7a1e9b2d4c3a8e6f1b2c3d4e5f60"));
        Assert.Equal("Invalid response data: \"invoices[0].invoiceNumber\" must not contain a \"&key=\" sequence", ex.Message);
    }
}