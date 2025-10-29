using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PayOS.Models.V2.PaymentRequests;

/// <summary>
/// Custom JSON converter for PaymentLinkStatus enum
/// </summary>
public class PaymentLinkStatusConverter : JsonConverter<PaymentLinkStatus>
{
    public override PaymentLinkStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return value?.ToUpper() switch
        {
            "PENDING" => PaymentLinkStatus.Pending,
            "CANCELLED" => PaymentLinkStatus.Cancelled,
            "UNDERPAID" => PaymentLinkStatus.Underpaid,
            "PAID" => PaymentLinkStatus.Paid,
            "EXPIRED" => PaymentLinkStatus.Expired,
            "PROCESSING" => PaymentLinkStatus.Processing,
            "FAILED" => PaymentLinkStatus.Failed,
            _ => throw new JsonException($"Unknown PaymentLinkStatus value: {value}")
        };
    }

    public override void Write(Utf8JsonWriter writer, PaymentLinkStatus value, JsonSerializerOptions options)
    {
        string stringValue = value switch
        {
            PaymentLinkStatus.Pending => "PENDING",
            PaymentLinkStatus.Cancelled => "CANCELLED",
            PaymentLinkStatus.Underpaid => "UNDERPAID",
            PaymentLinkStatus.Paid => "PAID",
            PaymentLinkStatus.Expired => "EXPIRED",
            PaymentLinkStatus.Processing => "PROCESSING",
            PaymentLinkStatus.Failed => "FAILED",
            _ => throw new JsonException($"Unknown PaymentLinkStatus value: {value}")
        };
        writer.WriteStringValue(stringValue);
    }

    public static string ToSerializedString(PaymentLinkStatus value)
    {
        return value switch
        {
            PaymentLinkStatus.Pending => "PENDING",
            PaymentLinkStatus.Cancelled => "CANCELLED",
            PaymentLinkStatus.Underpaid => "UNDERPAID",
            PaymentLinkStatus.Paid => "PAID",
            PaymentLinkStatus.Expired => "EXPIRED",
            PaymentLinkStatus.Processing => "PROCESSING",
            PaymentLinkStatus.Failed => "FAILED",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}

/// <summary>
/// Payment link status enumeration
/// </summary>
[JsonConverter(typeof(PaymentLinkStatusConverter))]
public enum PaymentLinkStatus
{
    Pending,
    Cancelled,
    Underpaid,
    Paid,
    Expired,
    Processing,
    Failed
}

/// <summary>
/// Tax percentage enumeration
/// </summary>
public enum TaxPercentage
{
    MinusTwo = -2,
    MinusOne = -1,
    Zero = 0,
    Five = 5,
    Eight = 8,
    Ten = 10
}

/// <summary>
/// Payment link item
/// </summary>
public class PaymentLinkItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public long Price { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("taxPercentage")]
    public TaxPercentage? TaxPercentage { get; set; }
}

/// <summary>
/// Invoice request details
/// </summary>
public class InvoiceRequest
{
    [JsonPropertyName("buyerNotGetInvoice")]
    public bool? BuyerNotGetInvoice { get; set; }

    [JsonPropertyName("taxPercentage")]
    public TaxPercentage? TaxPercentage { get; set; }
}

/// <summary>
/// Request to create a payment link
/// </summary>
public class CreatePaymentLinkRequest
{
    [JsonPropertyName("orderCode")]
    public long OrderCode { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("cancelUrl")]
    public string CancelUrl { get; set; } = "";

    [JsonPropertyName("returnUrl")]
    public string ReturnUrl { get; set; } = "";

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }

    [JsonPropertyName("items")]
    public List<PaymentLinkItem>? Items { get; set; }

    [JsonPropertyName("buyerName")]
    public string? BuyerName { get; set; }

    [JsonPropertyName("buyerCompanyName")]
    public string? BuyerCompanyName { get; set; }

    [JsonPropertyName("buyerEmail")]
    public string? BuyerEmail { get; set; }

    [JsonPropertyName("buyerPhone")]
    public string? BuyerPhone { get; set; }

    [JsonPropertyName("buyerAddress")]
    public string? BuyerAddress { get; set; }
    [JsonPropertyName("buyerTaxCode")]
    public string? BuyerTaxCode { get; set; }

    [JsonPropertyName("expiredAt")]
    public long? ExpiredAt { get; set; }

    [JsonPropertyName("invoice")]
    public InvoiceRequest? Invoice { get; set; }
}

/// <summary>
/// Response from creating a payment link
/// </summary>
public class CreatePaymentLinkResponse
{
    [JsonPropertyName("bin")]
    public string Bin { get; set; } = "";

    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; } = "";

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = "";

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("orderCode")]
    public long OrderCode { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";

    [JsonPropertyName("paymentLinkId")]
    public string PaymentLinkId { get; set; } = "";

    [JsonPropertyName("status")]
    public PaymentLinkStatus Status { get; set; }

    [JsonPropertyName("checkoutUrl")]
    public string CheckoutUrl { get; set; } = "";

    [JsonPropertyName("qrCode")]
    public string QrCode { get; set; } = "";
}

/// <summary>
/// Payment link information
/// </summary>
public class PaymentLink
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("orderCode")]
    public long OrderCode { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("amountPaid")]
    public long AmountPaid { get; set; }

    [JsonPropertyName("amountRemaining")]
    public long AmountRemaining { get; set; }

    [JsonPropertyName("status")]
    public PaymentLinkStatus Status { get; set; }

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = "";

    [JsonPropertyName("transactions")]
    public List<PaymentTransaction>? Transactions { get; set; }

    [JsonPropertyName("cancellationReason")]
    public string? CancellationReason { get; set; }

    [JsonPropertyName("canceledAt")]
    public string? CanceledAt { get; set; }
}

/// <summary>
/// Payment transaction details
/// </summary>
public class PaymentTransaction
{
    [JsonPropertyName("reference")]
    public string Reference { get; set; } = "";

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("transactionDateTime")]
    public string TransactionDateTime { get; set; } = "";

    [JsonPropertyName("virtualAccountName")]
    public string? VirtualAccountName { get; set; }

    [JsonPropertyName("virtualAccountNumber")]
    public string? VirtualAccountNumber { get; set; }

    [JsonPropertyName("counterAccountBankId")]
    public string? CounterAccountBankId { get; set; }

    [JsonPropertyName("counterAccountBankName")]
    public string? CounterAccountBankName { get; set; }

    [JsonPropertyName("counterAccountName")]
    public string? CounterAccountName { get; set; }

    [JsonPropertyName("counterAccountNumber")]
    public string? CounterAccountNumber { get; set; }
}

/// <summary>
/// Request to cancel a payment link
/// </summary>
public class CancelPaymentLinkRequest
{
    [JsonPropertyName("cancellationReason")]
    public string? CancellationReason { get; set; }
}