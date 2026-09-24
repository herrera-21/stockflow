using System.ComponentModel.DataAnnotations;
using StockFlow.Domain;

namespace StockFlow.Web.Validation;

/// <summary>
/// Validates that a person name only contains letters, spaces and the characters . ' -.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PersonNameAttribute : ValidationAttribute
{
    /// <summary>
    /// Validates the name. A blank value is left to the required validator.
    /// </summary>
    /// <param name="value">Name being validated.</param>
    /// <param name="validationContext">Context of the validation.</param>
    /// <returns>Success, or the validation error.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name))
        {
            return ValidationResult.Success;
        }

        return FieldValidation.IsValidPersonName(name)
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
    }
}
