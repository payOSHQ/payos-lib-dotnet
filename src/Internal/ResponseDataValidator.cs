using System;
using System.Text.Json;
using System.Text.RegularExpressions;

using PayOS.Exceptions;

namespace PayOS.Internal;

/// <summary>
/// Validates the shape of the raw <c>data</c> of a body-signed response before its signature is checked.
/// The body signature is computed over key=value pairs joined by '&amp;' without any encoding, with lists
/// serialised as JSON. The received data must therefore not be able to shift delimiters, replace a scalar
/// with an object, or turn a list into a string. Top-level values are scalars or arrays; objects are only
/// accepted inside arrays and may only contain scalars.
/// </summary>
internal static class ResponseDataValidator
{
    // A "&key=" sequence inside a value would let the unencoded signed string be split into other fields.
    private static readonly Regex KeyDelimiterPattern = new("&[A-Za-z0-9_]+=", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex KeyPattern = new(@"\A[A-Za-z0-9_]+\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static InvalidSignatureException Invalid(string reason) => new($"Invalid response data: {reason}");

    internal static void ValidateResponseContent(string responseContent)
    {
        using var document = JsonDocument.Parse(responseContent);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("data", out var data))
        {
            throw Invalid("data must be a JSON object");
        }
        Validate(data);
    }

    internal static void Validate(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
            throw Invalid("data must be a JSON object");

        foreach (var property in data.EnumerateObject())
        {
            ValidateKey(string.Empty, property.Name);

            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                ValidateScalar(property.Name, property.Value);
                continue;
            }

            var index = 0;
            foreach (var item in property.Value.EnumerateArray())
            {
                var path = $"{property.Name}[{index++}]";
                if (item.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in item.EnumerateObject())
                    {
                        ValidateKey(path + ".", field.Name);
                        ValidateScalar(path + "." + field.Name, field.Value);
                    }
                }
                else
                {
                    ValidateScalar(path, item);
                }
            }
        }
    }

    private static void ValidateKey(string path, string key)
    {
        if (!KeyPattern.IsMatch(key) || key == "__proto__")
            throw Invalid($"unsupported key \"{path}{key}\"");
    }

    private static void ValidateScalar(string path, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.True:
            case JsonValueKind.False:
                break;
            case JsonValueKind.Number:
                if (value.TryGetInt64(out var n))
                {
                    if (n > MaxSafeNumberValidator.MaxSafeNumber || n < -MaxSafeNumberValidator.MaxSafeNumber)
                        throw Invalid($"\"{path}\" must be a safe integer");
                }
                else if (!value.TryGetDouble(out var d) || double.IsNaN(d) || double.IsInfinity(d) ||
                         (d == Math.Floor(d) && Math.Abs(d) > MaxSafeNumberValidator.MaxSafeNumber))
                {
                    throw Invalid($"\"{path}\" must be a finite number within the safe integer range");
                }
                break;
            case JsonValueKind.String:
                if (KeyDelimiterPattern.IsMatch(value.GetString() ?? string.Empty))
                    throw Invalid($"\"{path}\" must not contain a \"&key=\" sequence");
                break;
            default:
                throw Invalid($"\"{path}\" has an unsupported type");
        }
    }
}