using System.ComponentModel.DataAnnotations;
using StockFlow.Domain;

namespace StockFlow.Web.Validation;

/// <summary>
/// Validates that the purchase unit factor is 1 when the purchase unit equals the base unit. Both
/// units are read from other properties of the same model.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PurchaseUnitFactorAttribute : ValidationAttribute
{
    // Names of the properties that hold the base and purchase units.
    private readonly string _baseUnitProperty;
    private readonly string _purchaseUnitProperty;

    /// <summary>Initializes the attribute.</summary>
    /// <param name="baseUnitProperty">Name of the property that holds the base unit.</param>
    /// <param name="purchaseUnitProperty">Name of the property that holds the purchase unit.</param>
    public PurchaseUnitFactorAttribute(string baseUnitProperty, string purchaseUnitProperty)
    {
        _baseUnitProperty = baseUnitProperty;
        _purchaseUnitProperty = purchaseUnitProperty;
    }

    /// <summary>
    /// Validates the factor against the selected units. Missing values are left to the required
    /// validator.
    /// </summary>
    /// <param name="value">Factor being validated.</param>
    /// <param name="validationContext">Context of the validation.</param>
    /// <returns>Success, or the validation error.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not decimal factor)
        {
            return ValidationResult.Success;
        }

        var instance = validationContext.ObjectInstance;
        var type = instance.GetType();

        if (type.GetProperty(_baseUnitProperty)?.GetValue(instance) is UnitOfMeasure baseUnit
            && type.GetProperty(_purchaseUnitProperty)?.GetValue(instance) is UnitOfMeasure purchaseUnit
            && baseUnit == purchaseUnit
            && factor != 1)
        {
            return new ValidationResult(
                FormatErrorMessage(validationContext.DisplayName),
                [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}
