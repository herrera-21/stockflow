using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Categories;

namespace StockFlow.Web.Localization;

/// <summary>
/// Helpers to display and list categories in the UI language.
/// </summary>
public static class CategoryLocalizationExtensions
{
    /// <summary>
    /// Resolves the display name of a category: the localized resource for the seeded codes
    /// (<c>Category_&lt;code&gt;</c>) when available, otherwise the stored name. User-created
    /// categories have no code and are shown with their name.
    /// </summary>
    /// <param name="localizer">Localizer used to look up the resource.</param>
    /// <param name="code">Stable category code; null for user-created categories.</param>
    /// <param name="name">Stored category name used as fallback.</param>
    /// <returns>The display name to render.</returns>
    public static string CategoryName(this IStringLocalizer<SharedResource> localizer, string? code, string name)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            var localized = localizer["Category_" + code];
            if (!localized.ResourceNotFound)
            {
                return localized.Value;
            }
        }

        return name;
    }

    /// <summary>
    /// Projects categories into the select items used by the product forms and filters, with their
    /// localized display names.
    /// </summary>
    /// <param name="localizer">Localizer used to look up the resource.</param>
    /// <param name="categories">Categories to project.</param>
    /// <returns>The select items, preserving the order of <paramref name="categories"/>.</returns>
    public static IReadOnlyList<SelectListItem> ToCategorySelectList(
        this IStringLocalizer<SharedResource> localizer,
        IEnumerable<CategoryDto> categories)
    {
        return categories
            .Select(c => new SelectListItem(localizer.CategoryName(c.Code, c.Name), c.Id.ToString()))
            .ToList();
    }
}
