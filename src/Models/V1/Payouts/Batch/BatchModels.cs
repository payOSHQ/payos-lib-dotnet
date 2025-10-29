using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PayOS.Models.V1.Payouts.Batch;

/// <summary>
/// Batch payout item
/// </summary>
public class PayoutBatchItem
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
}

/// <summary>
/// Batch payout request
/// </summary>
public class PayoutBatchRequest
{
    [JsonPropertyName("referenceId")]
    public string ReferenceId { get; set; } = "";

    [JsonPropertyName("validateDestination")]
    public bool? ValidateDestination { get; set; }

    [JsonPropertyName("category")]
    public List<string>? Category { get; set; }

    [JsonPropertyName("payouts")]
    public List<PayoutBatchItem> Payouts { get; set; } = [];
}