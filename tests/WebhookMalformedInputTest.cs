using System.Text.Json;

using PayOS.Exceptions;

using Xunit;

namespace PayOS.Tests;

public class WebhookMalformedInputTest
{
    // JSON escape for an unpaired high surrogate: parses, but fails when decoded to a string.
    private const string LoneSurrogate = "\\uD800";

    private static PayOSClient CreateClient() =>
        new(new PayOSOptions { ClientId = "c", ApiKey = "a", ChecksumKey = "k" });

    private static string ValidBody()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "webhook-vectors.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("cases").EnumerateArray()
            .First(c => c.GetProperty("expect").GetString() == "accept")
            .GetProperty("body").GetString()!;
    }

    [Fact]
    public async Task UnpairedSurrogateInDataValueThrowsWebhookException()
    {
        var body = "{\"data\":{\"description\":\"" + LoneSurrogate + "\"},\"signature\":\"x\"}";
        var ex = await Assert.ThrowsAsync<WebhookException>(() => CreateClient().Webhooks.VerifyAsync(body));
        Assert.StartsWith("Invalid webhook data", ex.Message);
    }

    [Fact]
    public async Task UnpairedSurrogateInDataKeyThrowsWebhookException()
    {
        var body = "{\"data\":{\"" + LoneSurrogate + "\":\"x\"},\"signature\":\"x\"}";
        var ex = await Assert.ThrowsAsync<WebhookException>(() => CreateClient().Webhooks.VerifyAsync(body));
        Assert.StartsWith("Invalid webhook data", ex.Message);
    }

    [Fact]
    public async Task UnpairedSurrogateInRootKeyThrowsWebhookException()
    {
        var body = ValidBody().Insert(1, "\"" + LoneSurrogate + "\":1,");
        var ex = await Assert.ThrowsAsync<WebhookException>(() => CreateClient().Webhooks.VerifyAsync(body));
        Assert.StartsWith("Invalid webhook data", ex.Message);
    }

    [Fact]
    public async Task UnpairedSurrogateInSignatureThrowsWebhookException()
    {
        var body = ValidBody();
        var signatureStart = body.IndexOf("\"signature\":\"", StringComparison.Ordinal) + "\"signature\":\"".Length;
        var signatureEnd = body.IndexOf('"', signatureStart);
        var tampered = body.Substring(0, signatureStart) + LoneSurrogate + body.Substring(signatureEnd);
        var ex = await Assert.ThrowsAsync<WebhookException>(() => CreateClient().Webhooks.VerifyAsync(tampered));
        Assert.Equal("Invalid signature", ex.Message);
    }
}
