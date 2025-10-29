using System.Text.Json.Serialization;

namespace PayOS.Models;

/// <summary>
/// Standard API response wrapper
/// </summary>
/// <typeparam name="T">Type of the response data</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Response code indicating success or error type
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Human-readable description of the response
    /// </summary>
    [JsonPropertyName("desc")]
    public string? Desc { get; set; }

    /// <summary>
    /// The actual response data
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Signature for response verification (optional)
    /// </summary>
    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}