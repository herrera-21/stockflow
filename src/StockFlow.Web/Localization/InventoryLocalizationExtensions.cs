using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using StockFlow.Domain;

namespace StockFlow.Web.Localization;

/// <summary>
/// Helpers to display and list inventory movement types and adjustment reasons in the UI language.
/// </summary>
public static class InventoryLocalizationExtensions
{
    /// <summary>
    /// Resolves the localized display name of a movement type (resource
    /// <c>MovementType&lt;Type&gt;</c>).
    /// </summary>
    /// <param name="localizer">Localizer used to look up the resource.</param>
    /// <param name="type">Movement type.</param>
    /// <returns>The localized movement type name.</returns>
    public static string MovementTypeName(
        this IStringLocalizer<SharedResource> localizer,
        InventoryMovementType type) =>
        localizer["MovementType" + type].Value;

    /// <summary>
    /// Resolves the localized display name of an adjustment reason (resource
    /// <c>AdjustmentReason&lt;Reason&gt;</c>).
    /// </summary>
    /// <param name="localizer">Localizer used to look up the resource.</param>
    /// <param name="reason">Adjustment reason.</param>
    /// <returns>The localized reason name.</returns>
    public static string AdjustmentReasonName(
        this IStringLocalizer<SharedResource> localizer,
        InventoryAdjustmentReason reason) =>
        localizer["AdjustmentReason" + reason].Value;

    /// <summary>
    /// Builds the select items for the two manual adjustment directions (increase and decrease).
    /// </summary>
    /// <param name="localizer">Localizer used to look up the resources.</param>
    /// <returns>The localized select items.</returns>
    public static IReadOnlyList<SelectListItem> ToAdjustmentTypeSelectList(
        this IStringLocalizer<SharedResource> localizer) =>
    [
        new(localizer.MovementTypeName(InventoryMovementType.AdjustmentIncrease),
            InventoryMovementType.AdjustmentIncrease.ToString()),
        new(localizer.MovementTypeName(InventoryMovementType.AdjustmentDecrease),
            InventoryMovementType.AdjustmentDecrease.ToString())
    ];

    /// <summary>
    /// Builds the select items for all adjustment reasons, in declaration order.
    /// </summary>
    /// <param name="localizer">Localizer used to look up the resource.</param>
    /// <returns>The localized select items.</returns>
    public static IReadOnlyList<SelectListItem> ToAdjustmentReasonSelectList(
        this IStringLocalizer<SharedResource> localizer) =>
        Enum.GetValues<InventoryAdjustmentReason>()
            .Select(reason => new SelectListItem(localizer.AdjustmentReasonName(reason), reason.ToString()))
            .ToList();
}
