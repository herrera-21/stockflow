using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using StockFlow.Domain;

namespace StockFlow.Web.Localization;

/// <summary>
/// Helpers to list identity document types in the UI language.
/// </summary>
public static class DocumentTypeLocalizationExtensions
{
    /// <summary>
    /// Resolves the localized display name of an identity document type.
    /// </summary>
    /// <param name="localizer">Localizer used to look up the resource.</param>
    /// <param name="type">Document type.</param>
    /// <returns>The localized type name.</returns>
    public static string DocumentTypeName(this IStringLocalizer<SharedResource> localizer, DocumentType type) =>
        localizer["DocumentType" + type].Value;

    /// <summary>
    /// Builds the select items for the identity document types, ordered DUI, NIT, passport, other.
    /// </summary>
    /// <param name="localizer">Localizer used to look up the resource.</param>
    /// <returns>The localized select items.</returns>
    public static IReadOnlyList<SelectListItem> ToDocumentTypeSelectList(
        this IStringLocalizer<SharedResource> localizer)
    {
        return
        [
            new SelectListItem(localizer["DocumentTypeDui"].Value, nameof(DocumentType.Dui)),
            new SelectListItem(localizer["DocumentTypeNit"].Value, nameof(DocumentType.Nit)),
            new SelectListItem(localizer["DocumentTypePassport"].Value, nameof(DocumentType.Passport)),
            new SelectListItem(localizer["DocumentTypeOther"].Value, nameof(DocumentType.Other))
        ];
    }
}
