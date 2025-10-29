using System.Text.Json.Serialization;

namespace PayOS.Models.V1.PayoutsAccount;

/// <summary>
/// Payout account information
/// </summary>
public class PayoutAccountInfo
{
    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; } = "";

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = "";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";

    [JsonPropertyName("balance")]
    public string Balance { get; set; } = "";
}