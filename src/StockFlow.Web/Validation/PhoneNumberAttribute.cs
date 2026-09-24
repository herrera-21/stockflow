using System.ComponentModel.DataAnnotations;
using StockFlow.Domain;

namespace StockFlow.Web.Validation;

/// <summary>
/// Validates that a phone number only contains digits, spaces and the characters + - ( ).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PhoneNumberAttribute : ValidationAttribute
{
    /// <summary>
    /// Validates the phone number. A blank value is valid because the field is optional.
    /// </summary>
    /// <param name="value">Phone number being validated.</param>
    /// <param name="validationContext">Context of the validation.</param>
    /// <returns>Success, or the validation error.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string phone || string.IsNullOrWhiteSpace(phone))
        {
            return ValidationResult.Success;
        }

        return FieldValidation.IsValidPhone(phone)
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
    }
}
