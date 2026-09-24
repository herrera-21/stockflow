using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace StockFlow.Web.Validation;

/// <summary>
/// Attribute adapter that turns the <see cref="ValidationAttribute.ErrorMessage"/> key of the custom
/// validators into a localized message. Without it ASP.NET Core renders the raw key to the user.
/// </summary>
internal sealed class LocalizedValidationAttributeAdapter : AttributeAdapterBase<ValidationAttribute>
{
    /// <summary>Initializes the adapter.</summary>
    /// <param name="attribute">Attribute being adapted.</param>
    /// <param name="stringLocalizer">Localizer used to resolve the message key.</param>
    public LocalizedValidationAttributeAdapter(ValidationAttribute attribute, IStringLocalizer? stringLocalizer)
        : base(attribute, stringLocalizer)
    {
    }

    /// <summary>
    /// Adds no client-side validation because the custom attributes are validated on the server only.
    /// </summary>
    /// <param name="context">Client validation context.</param>
    public override void AddValidation(ClientModelValidationContext context)
    {
    }

    /// <summary>
    /// Returns the localized and formatted error message using the attribute key and the field name.
    /// </summary>
    /// <param name="validationContext">Validation context of the model being validated.</param>
    /// <returns>The localized error message.</returns>
    public override string GetErrorMessage(ModelValidationContextBase validationContext)
        => GetErrorMessage(validationContext.ModelMetadata, validationContext.ModelMetadata.GetDisplayName());
}
