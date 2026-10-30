using System;

using PayOS.Exceptions;

namespace PayOS.Internal;

/// <summary>
/// Safe path segments for identifiers interpolated into API paths.
/// Without encoding, a value like <c>../../v1/payouts-account/balance</c> or <c>x?</c> re-targets the
/// authenticated request to another endpoint, so strings are percent-encoded and "." / ".." segments are rejected.
/// </summary>
internal static class PathSegment
{
    private const int MaxLength = 2048;

    public static string Encode(string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new PayOSException($"{name} must be a non-empty string");
        }
        if (value == "." || value == "..")
        {
            throw new PayOSException($"{name} must not be a dot segment");
        }
        if (value!.Length > MaxLength)
        {
            throw new PayOSException($"{name} is too long");
        }
        return Uri.EscapeDataString(value);
    }

    public static string Encode(string name, long value)
    {
        if (value < 0 || value > 9007199254740991L)
        {
            throw new PayOSException($"{name} must be a non-negative safe integer");
        }
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
