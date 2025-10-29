namespace PayOS.Models;

/// <summary>
/// Signature configuration options for requests and responses
/// </summary>
public class SignatureOptions
{
    /// <summary>
    /// Request signature type
    /// </summary>
    public string? Request { get; set; }

    /// <summary>
    /// Response signature type
    /// </summary>
    public string? Response { get; set; }
}

/// <summary>
/// Supported request signature types
/// </summary>
public static class RequestSignatureTypes
{
    /// <summary>
    /// Add signature to request body
    /// </summary>
    public const string Body = "body";

    /// <summary>
    /// Create payment link specific signature
    /// </summary>
    public const string CreatePaymentLink = "create-payment-link";

    /// <summary>
    /// Add signature to request header
    /// </summary>
    public const string Header = "header";
}

/// <summary>
/// Supported response signature types
/// </summary>
public static class ResponseSignatureTypes
{
    /// <summary>
    /// Verify signature from response body
    /// </summary>
    public const string Body = "body";

    /// <summary>
    /// Verify signature from response header
    /// </summary>
    public const string Header = "header";
}