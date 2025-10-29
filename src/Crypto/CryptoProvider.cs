using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PayOS.Crypto;

/// <summary>
/// Signature options for customizing HMAC signature generation
/// </summary>
public class CryptoSignatureOptions
{
    /// <summary>
    /// Whether to URL encode the query string parameters (default: true)
    /// </summary>
    public bool EncodeUri { get; set; } = true;

    /// <summary>
    /// Whether to sort arrays in the data (default: false)
    /// </summary>
    public bool SortArrays { get; set; } = false;

    /// <summary>
    /// HMAC algorithm to use (default: SHA256)
    /// </summary>
    public string Algorithm { get; set; } = "SHA256";
}

/// <summary>
/// Default implementation of crypto provider
/// </summary>
public class CryptoProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly string[] PaymentRequestRequiredFields = { "amount", "cancelUrl", "description", "orderCode", "returnUrl" };
    /// <summary>
    /// Create HMAC signature from object data (for 'body' signature type)
    /// </summary>
    /// <param name="data">The data object to sign</param>
    /// <param name="key">The secret key for HMAC generation</param>
    /// <returns>The signature or null if input is invalid</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
    /// <exception cref="ArgumentException">Thrown when key is null or empty</exception>
    public string? CreateSignatureFromObject(object data, string key)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            // Convert object to sorted query string
            var queryString = ConvertToSortedQueryString(data, encodeUri: false);
            return CreateHmac("SHA256", key, queryString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create signature from object", ex);
        }
    }

    /// <summary>
    /// Create HMAC signature for payment request using specific fields (for 'create-payment-link' signature type)
    /// </summary>
    /// <param name="data">The payment request data containing required fields</param>
    /// <param name="key">The secret key for HMAC generation</param>
    /// <returns>The signature or null if input is invalid</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
    /// <exception cref="ArgumentException">Thrown when key is invalid or required fields are missing</exception>
    public string? CreateSignatureOfPaymentRequest(object data, string key)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            // Extract specific fields for payment request signature
            var jsonElement = JsonSerializer.SerializeToElement(data);

            var requiredFields = new[] { "amount", "cancelUrl", "description", "orderCode", "returnUrl" };
            var missingFields = requiredFields.Where(field => !jsonElement.TryGetProperty(field, out _)).ToList();

            if (missingFields.Any())
            {
                throw new ArgumentException($"Payment request data missing required fields: {string.Join(", ", missingFields)}");
            }

            var amount = jsonElement.GetProperty("amount").GetInt64();
            var cancelUrl = jsonElement.GetProperty("cancelUrl").GetString() ?? "";
            var description = jsonElement.GetProperty("description").GetString() ?? "";
            var orderCode = jsonElement.GetProperty("orderCode").GetInt64();
            var returnUrl = jsonElement.GetProperty("returnUrl").GetString() ?? "";

            var dataStr = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
            return CreateHmac("SHA256", key, dataStr);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create payment request signature", ex);
        }
    }

    /// <summary>
    /// Create HMAC signature from JSON data (for 'header' signature type)
    /// </summary>
    /// <param name="secretKey">Secret key for HMAC signature generation</param>
    /// <param name="jsonData">JSON object data to be signed</param>
    /// <param name="options">Configuration options for signature generation</param>
    /// <returns>HMAC signature in hexadecimal format</returns>
    /// <exception cref="ArgumentNullException">Thrown when secretKey or jsonData is null</exception>
    /// <exception cref="ArgumentException">Thrown when secretKey is empty</exception>
    public string CreateSignature(string secretKey, object jsonData, CryptoSignatureOptions? options = null)
    {
        if (secretKey == null)
            throw new ArgumentNullException(nameof(secretKey));
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new ArgumentException("Secret key cannot be empty", nameof(secretKey));
        if (jsonData == null)
            throw new ArgumentNullException(nameof(jsonData));

        try
        {
            options ??= new CryptoSignatureOptions();
            var queryString = ConvertToAdvancedQueryString(jsonData, options);
            return CreateHmac(options.Algorithm, secretKey, queryString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create signature", ex);
        }
    }

    /// <summary>
    /// Generate a UUID v4
    /// </summary>
    public string CreateUuidv4()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Create HMAC signature using specified algorithm
    /// </summary>
    /// <param name="algorithm">The HMAC algorithm to use</param>
    /// <param name="key">The secret key</param>
    /// <param name="data">The data to sign</param>
    /// <returns>HMAC signature in hexadecimal format</returns>
    /// <exception cref="ArgumentException">Thrown when algorithm, key, or data is invalid</exception>
    /// <exception cref="NotSupportedException">Thrown when algorithm is not supported</exception>
    private static string CreateHmac(string algorithm, string key, string data)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
            throw new ArgumentException("Algorithm cannot be null or empty", nameof(algorithm));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        using HMAC hmac = algorithm.ToUpperInvariant() switch
        {
            "SHA256" => new HMACSHA256(Encoding.UTF8.GetBytes(key)),
            "SHA1" => new HMACSHA1(Encoding.UTF8.GetBytes(key)),
            "SHA512" => new HMACSHA512(Encoding.UTF8.GetBytes(key)),
            _ => throw new NotSupportedException($"Algorithm '{algorithm}' is not supported. Supported algorithms: SHA256, SHA1, SHA512")
        };

        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Convert object to sorted query string format (for backward compatibility)
    /// </summary>
    /// <param name="data">The data object to convert</param>
    /// <param name="encodeUri">Whether to URL encode the parameters</param>
    /// <returns>Query string representation of the data</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
    private static string ConvertToSortedQueryString(object data, bool encodeUri = true)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var jsonElement = JsonSerializer.SerializeToElement(data);
        var parameters = new SortedDictionary<string, string>();

        FlattenJsonElement(jsonElement, "", parameters);

        return encodeUri
            ? string.Join("&", parameters.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"))
            : string.Join("&", parameters.Select(kvp =>
                $"{kvp.Key}={kvp.Value}"));
    }

    /// <summary>
    /// Convert object to query string with advanced options
    /// </summary>
    /// <param name="data">The data object to convert</param>
    /// <param name="options">Configuration options for conversion</param>
    /// <returns>Query string representation of the data</returns>
    /// <exception cref="ArgumentNullException">Thrown when data or options is null</exception>
    private static string ConvertToAdvancedQueryString(object data, CryptoSignatureOptions options)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var jsonElement = JsonSerializer.SerializeToElement(data);
        var sortedData = DeepSortObject(jsonElement, options.SortArrays);

        var queryParams = new List<string>(sortedData.Count);
        foreach (var kvp in sortedData)
        {
            var value = kvp.Value ?? "";
            var valueStr = value is bool boolValue
                ? (boolValue ? "true" : "false") // Boolean.ToString() => "True"/"False" not "true"/"false"
                : (value.ToString() ?? "");

            var param = options.EncodeUri
                ? $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(valueStr)}"
                : $"{kvp.Key}={valueStr}";

            queryParams.Add(param);
        }

        return string.Join("&", queryParams);
    }

    /// <summary>
    /// Deep sort object
    /// </summary>
    private static SortedDictionary<string, object?> DeepSortObject(JsonElement element, bool sortArrays)
    {
        var result = new SortedDictionary<string, object?>();

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var value = ConvertJsonElementToObject(property.Value, sortArrays);
                    result[property.Name] = value;
                }
                break;
            default:
                // For non-object types, we can't sort keys, so return empty
                break;
        }

        return result;
    }

    /// <summary>
    /// Convert JsonElement to appropriate object type
    /// </summary>
    private static object? ConvertJsonElementToObject(JsonElement element, bool sortArrays)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => JsonSerializer.Serialize(DeepSortObject(element, sortArrays)),
            JsonValueKind.Array => JsonSerializer.Serialize(SortArrayElements(element), JsonOptions),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longVal) ? longVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText(),
        };
    }

    /// <summary>
    /// Sort array elements
    /// </summary>
    private static List<object?> SortArrayElements(JsonElement arrayElement)
    {
        var sortedArray = new List<object?>();
        foreach (var item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                // Sort object properties before adding to array
                var sortedObject = SortJsonObject(item);
                sortedArray.Add(sortedObject);
            }
            else
            {
                // Non-object values are added as-is
                sortedArray.Add(ConvertJsonElementToValue(item));
            }
        }
        return sortedArray;
    }

    /// <summary>
    /// Recursively flatten JSON element into key-value pairs (for backward compatibility)
    /// </summary>
    private static void FlattenJsonElement(JsonElement element, string prefix, SortedDictionary<string, string> parameters)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenJsonElement(property.Value, key, parameters);
                }
                break;

            case JsonValueKind.Array:
                // Sort array elements and then JSON serialize
                var sortedArray = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        // Sort object properties before adding to array
                        var sortedObject = SortJsonObject(item);
                        sortedArray.Add(sortedObject);
                    }
                    else
                    {
                        // Non-object values are added as-is
                        sortedArray.Add(ConvertJsonElementToValue(item));
                    }
                }
                parameters[prefix] = JsonSerializer.Serialize(sortedArray, JsonOptions);
                break;

            case JsonValueKind.String:
                parameters[prefix] = element.GetString() ?? "";
                break;

            case JsonValueKind.Number:
                parameters[prefix] = element.TryGetInt64(out var longVal) ? longVal.ToString() : element.GetDouble().ToString();
                break;

            case JsonValueKind.True:
                parameters[prefix] = "true";
                break;

            case JsonValueKind.False:
                parameters[prefix] = "false";
                break;

            case JsonValueKind.Null:
                parameters[prefix] = "";
                break;
        }
    }

    /// <summary>
    /// Sort JSON object properties alphabetically
    /// </summary>
    private static SortedDictionary<string, object?> SortJsonObject(JsonElement element)
    {
        var sorted = new SortedDictionary<string, object?>();
        foreach (var property in element.EnumerateObject())
        {
            sorted[property.Name] = ConvertJsonElementToValue(property.Value);
        }
        return sorted;
    }

    /// <summary>
    /// Convert JsonElement to appropriate value type
    /// </summary>
    private static object? ConvertJsonElementToValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => SortJsonObject(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElementToValue).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longVal) ? longVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText(),
        };
    }
}