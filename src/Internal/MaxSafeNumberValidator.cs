using System;

namespace PayOS.Internal;

internal static class MaxSafeNumberValidator
{
    internal const long MaxSafeNumber = 9007199254740991L;

    internal static void EnsureOrderCodeWithinMaxSafeNumber(long orderCode, string paramName = "orderCode")
    {
        if (orderCode > MaxSafeNumber)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                orderCode,
                $"Order code must be less than or equal to {MaxSafeNumber}.");
        }
    }
}
