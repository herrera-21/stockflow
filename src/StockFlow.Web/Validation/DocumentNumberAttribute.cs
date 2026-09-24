using System.ComponentModel.DataAnnotations;
using StockFlow.Domain;

namespace StockFlow.Web.Validation;

/// <summary>
/// Validates that a document number matches the selected <see cref="DocumentType"/>. The type is
/// read from another property of the same model.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class DocumentNumberAttribute : ValidationAttribute
{
    // Name of the property that holds the document type.
    private readonly string _documentTypeProperty;

    /// <summary>Initializes the attribute.</summary>
    /// <param name="documentTypeProperty">Name of the property that holds the document type.</param>
    public DocumentNumberAttribute(string documentTypeProperty)
    {
        _documentTypeProperty = documentTypeProperty;
    }

    /// <summary>
    /// Validates the document number against the selected type. A blank value is left to the
    /// required validator.
    /// </summary>
    /// <param name="value">Document number being validated.</param>
    /// <param name="validationContext">Context of the validation.</param>
    /// <returns>Success, or the validation error.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string taxId || string.IsNullOrWhiteSpace(taxId))
        {
            return ValidationResult.Success;
        }

        var typeProperty = validationContext.ObjectInstance.GetType().GetProperty(_documentTypeProperty);
        if (typeProperty?.GetValue(validationContext.ObjectInstance) is DocumentType documentType
            && DocumentValidation.IsValid(documentType, taxId))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
    }
}
