using System.ComponentModel.DataAnnotations;
using StockFlow.Domain;

namespace StockFlow.Web.Validation;

/// <summary>
/// Validates that a quantity fits the unit of measure held by another property of the same model:
/// countable units (unit, dozen, box, pack) only accept whole numbers.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class QuantityForUnitAttribute : ValidationAttribute
{
    // Name of the property that holds the unit of measure.
    private readonly string _unitProperty;

    /// <summary>Initializes the attribute.</summary>
    /// <param name="unitProperty">Name of the property that holds the unit of measure.</param>
    public QuantityForUnitAttribute(string unitProperty)
    {
        _unitProperty = unitProperty;
    }

    /// <summary>
    /// Validates the quantity against the unit. A missing value or unit is left to the required
    /// validator.
    /// </summary>
    /// <param name="value">Quantity being validated.</param>
    /// <param name="validationContext">Context of the validation.</param>
    /// <returns>Success, or the validation error.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not decimal quantity)
        {
            return ValidationResult.Success;
        }

        var unitProperty = validationContext.ObjectInstance.GetType().GetProperty(_unitProperty);
        if (unitProperty?.GetValue(validationContext.ObjectInstance) is not UnitOfMeasure unit
            || unit.IsValidQuantity(quantity))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            FormatErrorMessage(validationContext.DisplayName),
            [validationContext.MemberName!]);
    }
}
