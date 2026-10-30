using System.Text.Json;

using PayOS.Exceptions;
using PayOS.Models.Webhooks;

using Xunit;

namespace PayOS.Tests;

public class WebhookVectorsTest
{
    private static readonly JsonElement Vectors = LoadVectors();

    private static JsonElement LoadVectors()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "webhook-vectors.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.Clone();
    }

    private static string ChecksumKey => Vectors.GetProperty("checksumKey").GetString()!;

    private static PayOSClient CreateClient() =>
        new(new PayOSOptions { ClientId = "c", ApiKey = "a", ChecksumKey = ChecksumKey });

    private static IEnumerable<object[]> SelectCases(bool typedOnly) =>
        Vectors.GetProperty("cases").EnumerateArray()
            .Where(c => !typedOnly || c.GetProperty("typed").GetBoolean())
            .Select(c => new object[]
            {
                c.GetProperty("id").GetString()!,
                c.GetProperty("expect").GetString()!,
                c.GetProperty("body").GetString()!
            });

    public static IEnumerable<object[]> Cases() => SelectCases(typedOnly: false);

    public static IEnumerable<object[]> TypedCases() => SelectCases(typedOnly: true);

    private static long ExpectedOrderCode(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("data").GetProperty("orderCode").GetInt64();
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task StringOverloadMatchesVector(string id, string expect, string body)
    {
        var client = CreateClient();
        if (expect == "accept")
        {
            var result = await client.Webhooks.VerifyAsync(body);
            Assert.Equal(ExpectedOrderCode(body), result.OrderCode);
            return;
        }

        var ex = await Assert.ThrowsAsync<WebhookException>(() => client.Webhooks.VerifyAsync(body));
        if (!id.StartsWith("R14") && !id.StartsWith("R15"))
        {
            Assert.StartsWith("Invalid webhook data", ex.Message);
        }
    }

    [Theory]
    [MemberData(nameof(TypedCases))]
    public async Task TypedOverloadMatchesVector(string id, string expect, string body)
    {
        var client = CreateClient();
        Webhook webhook;
        try
        {
            webhook = JsonSerializer.Deserialize<Webhook>(body)!;
        }
        catch (JsonException)
        {
            Assert.True(expect == "reject", $"{id}: deserialization failed for an accept case");
            return;
        }

        if (expect == "accept")
        {
            var result = await client.Webhooks.VerifyAsync(webhook);
            Assert.Equal(ExpectedOrderCode(body), result.OrderCode);
        }
        else
        {
            await Assert.ThrowsAsync<WebhookException>(() => client.Webhooks.VerifyAsync(webhook));
        }
    }

    private static string AcceptBody() =>
        Vectors.GetProperty("cases").EnumerateArray()
            .First(c => c.GetProperty("expect").GetString() == "accept")
            .GetProperty("body").GetString()!;

    [Fact]
    public async Task RejectsDuplicateDataKey()
    {
        var body = AcceptBody();
        var data = JsonDocument.Parse(body).RootElement.GetProperty("data").GetRawText();
        var tampered = body.Insert(1, "\"data\":" + data + ",");
        await Assert.ThrowsAsync<WebhookException>(() => CreateClient().Webhooks.VerifyAsync(tampered));
    }

    [Fact]
    public async Task RejectsDuplicateSignatureKey()
    {
        var body = AcceptBody();
        var tampered = body.Insert(1, "\"signature\":\"00\",");
        await Assert.ThrowsAsync<WebhookException>(() => CreateClient().Webhooks.VerifyAsync(tampered));
    }

    [Fact]
    public async Task RejectsDuplicateKeyInsideData()
    {
        var body = AcceptBody();
        var tampered = body.Replace("\"data\":{", "\"data\":{\"amount\":1,");
        await Assert.ThrowsAsync<WebhookException>(() => CreateClient().Webhooks.VerifyAsync(tampered));
    }
}
