namespace StockFlow.Domain;

/// <summary>
/// Light, shared validation rules for free-text contact fields. They keep obvious garbage out
/// (digits in a person name, letters in a phone) without blocking valid international values.
/// </summary>
public static class FieldValidation
{
    /// <summary>
    /// Checks a person name: Unicode letters, spaces and the characters . ' - only. Company names
    /// are intentionally not validated here because they legitimately contain digits and symbols.
    /// </summary>
    /// <param name="name">Name to validate.</param>
    /// <returns>True when the name only contains allowed characters.</returns>
    public static bool IsValidPersonName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.Trim().All(c => char.IsLetter(c) || char.IsWhiteSpace(c) || c is '.' or '\'' or '-');
    }

    /// <summary>
    /// Checks a phone number: digits, spaces and the characters + - ( ), between 7 and 20 characters.
    /// A blank value is considered valid because the field is optional.
    /// </summary>
    /// <param name="phone">Phone number to validate.</param>
    /// <returns>True when the phone is blank or matches the allowed pattern.</returns>
    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return true;
        }

        var value = phone.Trim();
        if (value.Length is < 7 or > 20)
        {
            return false;
        }

        return value.All(c => char.IsDigit(c) || c is ' ' or '+' or '-' or '(' or ')');
    }
}
