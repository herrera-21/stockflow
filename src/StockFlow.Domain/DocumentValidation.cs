namespace StockFlow.Domain;

/// <summary>
/// Rules and formatting for fiscal/identity documents. Shared by the domain entities and the web
/// validation so both sides agree.
/// </summary>
public static class DocumentValidation
{
    /// <summary>
    /// Checks whether the document number is valid for the given type. DUI is 9 digits; NIT is 9
    /// (homologated DUI) or 14 (legal entity) digits; passport and other are alphanumeric.
    /// </summary>
    /// <param name="type">Document type.</param>
    /// <param name="taxId">Document number.</param>
    /// <returns>True when the number matches the rules for the type.</returns>
    public static bool IsValid(DocumentType type, string? taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
        {
            return false;
        }

        var value = taxId.Trim();

        return type switch
        {
            DocumentType.Dui => IsNumeric(value, 9),
            DocumentType.Nit => IsNumeric(value, 9, 14),
            DocumentType.Passport => IsAlphanumeric(value, 5, 20),
            DocumentType.Other => IsAlphanumeric(value, 1, 50),
            _ => false
        };
    }

    /// <summary>
    /// Formats the document number for storage: DUI and NIT get their hyphens, the rest keep the
    /// trimmed value.
    /// </summary>
    /// <param name="type">Document type.</param>
    /// <param name="taxId">Document number.</param>
    /// <returns>The formatted document number.</returns>
    public static string Normalize(DocumentType type, string taxId)
    {
        var trimmed = taxId.Trim();

        if (type == DocumentType.Dui)
        {
            var digits = OnlyDigits(trimmed);
            return digits.Length == 9 ? $"{digits[..8]}-{digits[8]}" : trimmed;
        }

        if (type == DocumentType.Nit)
        {
            var digits = OnlyDigits(trimmed);
            return digits.Length switch
            {
                9 => $"{digits[..8]}-{digits[8]}",
                14 => $"{digits[..4]}-{digits.Substring(4, 6)}-{digits.Substring(10, 3)}-{digits[13]}",
                _ => trimmed
            };
        }

        return trimmed;
    }

    // True when the value only contains digits, hyphens and spaces, and has one of the given digit
    // counts.
    private static bool IsNumeric(string value, params int[] digitCounts)
    {
        var invalid = value.Any(c => !char.IsDigit(c) && c != '-' && c != ' ');
        return !invalid && digitCounts.Contains(OnlyDigits(value).Length);
    }

    // True when the value is alphanumeric and its length is within the given range.
    private static bool IsAlphanumeric(string value, int minLength, int maxLength)
    {
        if (value.Length < minLength || value.Length > maxLength)
        {
            return false;
        }

        return value.All(char.IsLetterOrDigit);
    }

    // Keeps only the digits of a value.
    private static string OnlyDigits(string value) => new(value.Where(char.IsDigit).ToArray());
}
