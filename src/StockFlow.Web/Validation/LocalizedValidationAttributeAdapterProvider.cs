using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace StockFlow.Web.Validation;

/// <summary>
/// Supplies a localized adapter for the custom validation attributes and defers to the framework
/// provider for the built-in ones, which are localized already.
/// </summary>
internal sealed class LocalizedValidationAttributeAdapterProvider : IValidationAttributeAdapterProvider
{
    // Framework provider used as a fallback for the attributes it already knows how to localize.
    private readonly ValidationAttributeAdapterProvider _fallback = new();

    /// <summary>Returns the adapter used to localize the error message of the given attribute.</summary>
    /// <param name="attribute">Attribute being adapted.</param>
    /// <param name="stringLocalizer">Localizer used to resolve the message key.</param>
    /// <returns>A localized adapter for the custom attributes; otherwise the framework adapter.</returns>
    public IAttributeAdapter? GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer? stringLocalizer)
    {
        if (attribute is PersonNameAttribute
            or PhoneNumberAttribute
            or DocumentNumberAttribute
            or PurchaseUnitFactorAttribute
            or QuantityForUnitAttribute)
        {
            return new LocalizedValidationAttributeAdapter(attribute, stringLocalizer);
        }

        return _fallback.GetAttributeAdapter(attribute, stringLocalizer);
    }
}
