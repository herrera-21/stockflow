using System.Globalization;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain;

/// <summary>
/// Business format of a purchase order number. The number is <c>OC-</c> followed by the sequence
/// value padded with zeros to six digits, for example <c>OC-000001</c>. The format lives in the
/// domain because it is a business rule; the atomic counter that feeds it comes from the database
/// sequence configured in Infrastructure.
/// </summary>
public static class PurchaseNumber
{
    /// <summary>Prefix that identifies a purchase order.</summary>
    public const string Prefix = "OC";

    /// <summary>Minimum number of digits the sequence value is padded to.</summary>
    public const int Digits = 6;

    /// <summary>
    /// Formats a sequence value as a purchase order number. Values with more than
    /// <see cref="Digits"/> digits keep every digit (no truncation).
    /// </summary>
    /// <param name="sequenceValue">Value read from the purchase order sequence; cannot be negative.</param>
    /// <returns>The formatted number, for example <c>OC-000001</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="sequenceValue"/> is negative.</exception>
    public static string Format(long sequenceValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sequenceValue);

        return $"{Prefix}-{sequenceValue.ToString($"D{Digits}", CultureInfo.InvariantCulture)}";
    }
}
