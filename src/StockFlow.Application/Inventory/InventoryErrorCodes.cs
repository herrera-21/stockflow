namespace StockFlow.Application.Inventory;

/// <summary>
/// Stable error codes returned by inventory use cases. They double as localization resource keys.
/// </summary>
public static class InventoryErrorCodes
{
    /// <summary>The requested product does not exist.</summary>
    public const string ProductNotFound = "ProductNotFound";

    /// <summary>Movements cannot be applied to an inactive product.</summary>
    public const string ProductInactive = "InventoryProductInactive";

    /// <summary>The quantity is zero or does not fit the product's base unit.</summary>
    public const string InvalidQuantity = "InventoryInvalidQuantity";

    /// <summary>A manual adjustment requires a reason.</summary>
    public const string AdjustmentReasonRequired = "InventoryAdjustmentReasonRequired";

    /// <summary>A note is required when the adjustment reason is "other".</summary>
    public const string AdjustmentNoteRequired = "InventoryAdjustmentNoteRequired";

    /// <summary>The movement would leave the stock below zero.</summary>
    public const string InsufficientStock = "InventoryInsufficientStock";

    /// <summary>The stock changed between reading and saving; the operation must be retried.</summary>
    public const string ConcurrencyConflict = "InventoryConcurrencyConflict";

    /// <summary>The movement type or reason is not valid for a manual adjustment.</summary>
    public const string InvalidMovement = "InventoryInvalidMovement";
}
