using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using StockFlow.Domain;

namespace StockFlow.Web.Localization;

/// <summary>
/// Helpers to display and list units of measure in the UI language.
/// </summary>
public static class UnitOfMeasureLocalizationExtensions
{
    /// <summary>
    /// Resolves the localized display name of a unit (resource <c>UnitOfMeasure&lt;Unit&gt;</c>).
    /// </summary>
    /// <param name="localizer">Localizer used to look up the resource.</param>
    /// <param name="unit">Unit of measure.</param>
    /// <returns>The localized unit name.</returns>
    public static string UnitName(this IStringLocalizer<SharedResource> localizer, UnitOfMeasure unit) =>
        localizer["UnitOfMeasure" + unit].Value;

    /// <summary>
    /// Resolves the localized short symbol of a unit (resource <c>UnitOfMeasureShort&lt;Unit&gt;</c>).
    /// </summary>
    /// <param name="localizer">Localizer used to look up the resource.</param>
    /// <param name="unit">Unit of measure.</param>
    /// <returns>The localized unit symbol, for example "lb".</returns>
    public static string UnitSymbol(this IStringLocalizer<SharedResource> localizer, UnitOfMeasure unit) =>
        localizer["UnitOfMeasureShort" + unit].Value;

    /// <summary>
    /// Formats a quantity followed by its unit symbol, without trailing zeros (for example "12.5 lb").
    /// </summary>
    /// <param name="localizer">Localizer used to look up the unit symbol.</param>
    /// <param name="quantity">Quantity to format.</param>
    /// <param name="unit">Unit the quantity is expressed in.</param>
    /// <returns>The formatted quantity.</returns>
    public static string FormatQuantity(
        this IStringLocalizer<SharedResource> localizer,
        decimal quantity,
        UnitOfMeasure unit) =>
        $"{quantity:0.###} {localizer.UnitSymbol(unit)}";

    /// <summary>
    /// Builds the select items for all units of measure, in declaration order (countable units
    /// first, then weight, volume and length).
    /// </summary>
    /// <param name="localizer">Localizer used to look up the resource.</param>
    /// <returns>The localized select items.</returns>
    public static IReadOnlyList<SelectListItem> ToUnitOfMeasureSelectList(
        this IStringLocalizer<SharedResource> localizer)
    {
        return Enum.GetValues<UnitOfMeasure>()
            .Select(unit => new SelectListItem(localizer.UnitName(unit), unit.ToString()))
            .ToList();
    }
}
