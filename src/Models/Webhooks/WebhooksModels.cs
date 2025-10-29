using System;
using System.Text.Json.Serialization;

namespace PayOS.Models.Webhooks;


/// <summary>
/// Webhook data received from PayOS
/// </summary>
public class Webhook
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("desc")]
    public string Description { get; set; } = "";

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public WebhookData Data { get; set; } = new();

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = "";
}

/// <summary>
/// Webhook data payload
/// </summary>
public class WebhookData
{
    [JsonPropertyName("orderCode")]
    public long OrderCode { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; } = "";

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = "";

    [JsonPropertyName("transactionDateTime")]
    public string TransactionDateTime { get; set; } = ""; // yyyy-MM-dd HH:mm:ss

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "VND";

    [JsonPropertyName("paymentLinkId")]
    public string PaymentLinkId { get; set; } = "";

    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("desc")]
    public string Description2 { get; set; } = "";

    [JsonPropertyName("counterAccountBankId")]
    public string? CounterAccountBankId { get; set; }

    [JsonPropertyName("counterAccountBankName")]
    public string? CounterAccountBankName { get; set; }

    [JsonPropertyName("counterAccountName")]
    public string? CounterAccountName { get; set; }

    [JsonPropertyName("counterAccountNumber")]
    public string? CounterAccountNumber { get; set; }

    [JsonPropertyName("virtualAccountName")]
    public string? VirtualAccountName { get; set; }

    [JsonPropertyName("virtualAccountNumber")]
    public string? VirtualAccountNumber { get; set; }
}

/// <summary>
/// Request to confirm/register a webhook URL
/// </summary>
public class ConfirmWebhookRequest
{
    [JsonPropertyName("webhookUrl")]
    public string WebhookUrl { get; set; } = "";
}

/// <summary>
/// Response from confirming a webhook URL
/// </summary>
public class ConfirmWebhookResponse
{
    [JsonPropertyName("webhookUrl")]
    public string WebhookUrl { get; set; } = "";

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = "";

    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("shortName")]
    public string ShortName { get; set; } = "";
}