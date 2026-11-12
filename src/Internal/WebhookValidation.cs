using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using PayOS.Exceptions;
using PayOS.Models.Webhooks;

namespace PayOS.Internal;

internal static class WebhookValidation
{
    private const long MaxSafeInteger = 9007199254740991L;
    private const long MinSafeInteger = -9007199254740991L;

    // \A...\z instead of ^...$: $ also matches before a trailing \n.
    private static readonly Regex KeyPattern = new(@"\A[A-Za-z0-9_]+\z", RegexOptions.CultureInvariant);

    private static readonly string[] CoreIntegerKeys = { "orderCode", "amount" };

    private static readonly string[] CoreStringKeys =
    {
        "description", "accountNumber", "reference", "transactionDateTime",
        "currency", "paymentLinkId", "code", "desc"
    };

    private static readonly string[] OptionalStringKeys =
    {
        "counterAccountBankId", "counterAccountBankName", "counterAccountName",
        "counterAccountNumber", "virtualAccountName", "virtualAccountNumber"
    };

    internal static bool ConstantTimeEquals(string? a, string? b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        var x = Encoding.UTF8.GetBytes(a);
        var y = Encoding.UTF8.GetBytes(b);
        if (x.Length != y.Length)
        {
            return false;
        }

        var diff = 0;
        for (var i = 0; i < x.Length; i++)
        {
            diff |= x[i] ^ y[i];
        }

        return diff == 0;
    }

    internal static void ValidateWireData(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
        {
            throw Invalid("data must be an object");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in data.EnumerateObject())
        {
            var name = property.Name;
            if (!seen.Add(name))
            {
                throw Invalid($"duplicate key {name}");
            }

            if (!KeyPattern.IsMatch(name) || name == "__proto__")
            {
                throw Invalid($"invalid key {name}");
            }

            var value = property.Value;
            switch (value.ValueKind)
            {
                case JsonValueKind.Null:
                    break;
                case JsonValueKind.String:
                    if (value.GetString()!.Contains("="))
                    {
                        throw Invalid($"value of {name} contains '='");
                    }
                    break;
                case JsonValueKind.Number:
                    if (!value.TryGetInt64(out var n) || n < MinSafeInteger || n > MaxSafeInteger)
                    {
                        throw Invalid($"value of {name} is not a safe integer");
                    }
                    break;
                default:
                    throw Invalid($"value of {name} has unsupported type");
            }

            values[name] = value;
        }

        foreach (var key in CoreIntegerKeys)
        {
            if (!values.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.Number)
            {
                throw Invalid($"{key} must be an integer");
            }
        }

        foreach (var key in CoreStringKeys)
        {
            if (!values.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.String)
            {
                throw Invalid($"{key} must be a string");
            }
        }

        foreach (var key in OptionalStringKeys)
        {
            if (values.TryGetValue(key, out var value)
                && value.ValueKind != JsonValueKind.String
                && value.ValueKind != JsonValueKind.Null)
            {
                throw Invalid($"{key} must be a string or null");
            }
        }
    }

    internal static void ValidateTyped(WebhookData d)
    {
        if (d.OrderCode < MinSafeInteger || d.OrderCode > MaxSafeInteger)
        {
            throw Invalid("orderCode is not a safe integer");
        }

        if (d.Amount < MinSafeInteger || d.Amount > MaxSafeInteger)
        {
            throw Invalid("amount is not a safe integer");
        }

        string?[] strings =
        {
            d.Description, d.AccountNumber, d.Reference, d.TransactionDateTime, d.Currency,
            d.PaymentLinkId, d.Code, d.Description2, d.CounterAccountBankId, d.CounterAccountBankName,
            d.CounterAccountName, d.CounterAccountNumber, d.VirtualAccountName, d.VirtualAccountNumber
        };
        foreach (var s in strings)
        {
            if (s != null && s.Contains("="))
            {
                throw Invalid("string value contains '='");
            }
        }
    }

    private static WebhookException Invalid(string reason)
    {
        return new WebhookException("Invalid webhook data: " + reason);
    }
}