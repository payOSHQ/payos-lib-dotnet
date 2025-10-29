using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PayOS.Models.V1.Payouts;

/// <summary>
/// Custom JSON converter for PayoutTransactionState enum
/// </summary>
public class PayoutTransactionStateConverter : JsonConverter<PayoutTransactionState>
{
    public override PayoutTransactionState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return value?.ToUpper() switch
        {
            "RECEIVED" => PayoutTransactionState.Received,
            "PROCESSING" => PayoutTransactionState.Processing,
            "CANCELLED" => PayoutTransactionState.Cancelled,
            "SUCCEEDED" => PayoutTransactionState.Succeeded,
            "ON_HOLD" => PayoutTransactionState.OnHold,
            "REVERSED" => PayoutTransactionState.Reversed,
            "FAILED" => PayoutTransactionState.Failed,
            _ => throw new JsonException($"Unknown PayoutTransactionState value: {value}")
        };
    }

    public override void Write(Utf8JsonWriter writer, PayoutTransactionState value, JsonSerializerOptions options)
    {
        string stringValue = value switch
        {
            PayoutTransactionState.Received => "RECEIVED",
            PayoutTransactionState.Processing => "PROCESSING",
            PayoutTransactionState.Cancelled => "CANCELLED",
            PayoutTransactionState.Succeeded => "SUCCEEDED",
            PayoutTransactionState.OnHold => "ON_HOLD",
            PayoutTransactionState.Reversed => "REVERSED",
            PayoutTransactionState.Failed => "FAILED",
            _ => throw new JsonException($"Unknown PayoutTransactionState value: {value}")
        };
        writer.WriteStringValue(stringValue);
    }

    public static string ToSerializedString(PayoutTransactionState value)
    {
        return value switch
        {
            PayoutTransactionState.Received => "RECEIVED",
            PayoutTransactionState.Processing => "PROCESSING",
            PayoutTransactionState.Cancelled => "CANCELLED",
            PayoutTransactionState.Succeeded => "SUCCEEDED",
            PayoutTransactionState.OnHold => "ON_HOLD",
            PayoutTransactionState.Reversed => "REVERSED",
            PayoutTransactionState.Failed => "FAILED",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}

/// <summary>
/// Custom JSON converter for PayoutApprovalState enum
/// </summary>
public class PayoutApprovalStateConverter : JsonConverter<PayoutApprovalState>
{
    public override PayoutApprovalState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return value?.ToUpper() switch
        {
            "DRAFTING" => PayoutApprovalState.Drafting,
            "SUBMITTED" => PayoutApprovalState.Submitted,
            "APPROVED" => PayoutApprovalState.Approved,
            "REJECTED" => PayoutApprovalState.Rejected,
            "CANCELLED" => PayoutApprovalState.Cancelled,
            "SCHEDULED" => PayoutApprovalState.Scheduled,
            "PROCESSING" => PayoutApprovalState.Processing,
            "FAILED" => PayoutApprovalState.Failed,
            "PARTIAL_COMPLETED" => PayoutApprovalState.PartialCompleted,
            "COMPLETED" => PayoutApprovalState.Completed,
            _ => throw new JsonException($"Unknown PayoutApprovalState value: {value}")
        };
    }

    public override void Write(Utf8JsonWriter writer, PayoutApprovalState value, JsonSerializerOptions options)
    {
        string stringValue = value switch
        {
            PayoutApprovalState.Drafting => "DRAFTING",
            PayoutApprovalState.Submitted => "SUBMITTED",
            PayoutApprovalState.Approved => "APPROVED",
            PayoutApprovalState.Rejected => "REJECTED",
            PayoutApprovalState.Cancelled => "CANCELLED",
            PayoutApprovalState.Scheduled => "SCHEDULED",
            PayoutApprovalState.Processing => "PROCESSING",
            PayoutApprovalState.Failed => "FAILED",
            PayoutApprovalState.PartialCompleted => "PARTIAL_COMPLETED",
            PayoutApprovalState.Completed => "COMPLETED",
            _ => throw new JsonException($"Unknown PayoutApprovalState value: {value}")
        };
        writer.WriteStringValue(stringValue);
    }

    public static string ToSerializedString(PayoutApprovalState value)
    {
        return value switch
        {
            PayoutApprovalState.Drafting => "DRAFTING",
            PayoutApprovalState.Submitted => "SUBMITTED",
            PayoutApprovalState.Approved => "APPROVED",
            PayoutApprovalState.Rejected => "REJECTED",
            PayoutApprovalState.Cancelled => "CANCELLED",
            PayoutApprovalState.Scheduled => "SCHEDULED",
            PayoutApprovalState.Processing => "PROCESSING",
            PayoutApprovalState.Failed => "FAILED",
            PayoutApprovalState.PartialCompleted => "PARTIAL_COMPLETED",
            PayoutApprovalState.Completed => "COMPLETED",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}

/// <summary>
/// Payout transaction state enumeration
/// </summary>
[JsonConverter(typeof(PayoutTransactionStateConverter))]
public enum PayoutTransactionState
{
    Received,
    Processing,
    Cancelled,
    Succeeded,
    OnHold,
    Reversed,
    Failed
}

/// <summary>
/// Payout approval state enumeration
/// </summary>
[JsonConverter(typeof(PayoutApprovalStateConverter))]
public enum PayoutApprovalState
{
    Drafting,
    Submitted,
    Approved,
    Rejected,
    Cancelled,
    Scheduled,
    Processing,
    Failed,
    PartialCompleted,
    Completed
}

/// <summary>
/// Payout request data
/// </summary>
public class PayoutRequest
{
    [JsonPropertyName("referenceId")]
    public string ReferenceId { get; set; } = "";

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("toBin")]
    public string ToBin { get; set; } = "";

    [JsonPropertyName("toAccountNumber")]
    public string ToAccountNumber { get; set; } = "";

    [JsonPropertyName("category")]
    public List<string>? Category { get; set; }
}

/// <summary>
/// Payout transaction details
/// </summary>
public class PayoutTransaction
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("referenceId")]
    public string ReferenceId { get; set; } = "";

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("toBin")]
    public string ToBin { get; set; } = "";

    [JsonPropertyName("toAccountNumber")]
    public string ToAccountNumber { get; set; } = "";

    [JsonPropertyName("toAccountName")]
    public string? ToAccountName { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("transactionDatetime")]
    public string? TransactionDatetime { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("state")]
    public PayoutTransactionState State { get; set; }
}

/// <summary>
/// Payout response data
/// </summary>
public class Payout
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("referenceId")]
    public string ReferenceId { get; set; } = "";

    [JsonPropertyName("transactions")]
    public List<PayoutTransaction> Transactions { get; set; } = new();

    [JsonPropertyName("category")]
    public List<string>? Category { get; set; }

    [JsonPropertyName("approvalState")]
    public PayoutApprovalState ApprovalState { get; set; }

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = "";
}

/// <summary>
/// Estimate credit response
/// </summary>
public class EstimateCredit
{
    [JsonPropertyName("estimateCredit")]
    public long EstimateCreditAmount { get; set; }
}

/// <summary>
/// Parameters for getting payout list
/// </summary>
public class GetPayoutListParam
{
    [JsonPropertyName("referenceId")]
    public string? ReferenceId { get; set; }

    [JsonPropertyName("approvalState")]
    public PayoutApprovalState? ApprovalState { get; set; }

    [JsonPropertyName("category")]
    public List<string>? Category { get; set; }

    [JsonPropertyName("fromDate")]
    public string? FromDate { get; set; }

    [JsonPropertyName("toDate")]
    public string? ToDate { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("offset")]
    public int? Offset { get; set; }
}

/// <summary>
/// Payout list response
/// </summary>
public class PayoutListResponse
{
    [JsonPropertyName("pagination")]
    public Pagination Pagination { get; set; } = new();

    [JsonPropertyName("payouts")]
    public List<Payout> Payouts { get; set; } = [];
}

/// <summary>
/// Pagination information
/// </summary>
public class Pagination
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }
}