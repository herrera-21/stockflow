namespace StockFlow.Domain;

/// <summary>
/// Business rules attached to <see cref="InventoryMovementType"/> values.
/// </summary>
public static class InventoryMovementTypeExtensions
{
    /// <summary>
    /// Whether the movement increases stock. Inbound movements add the quantity; outbound movements
    /// subtract it.
    /// </summary>
    /// <param name="type">Movement type to check.</param>
    /// <returns>True when the movement adds stock.</returns>
    public static bool IsInbound(this InventoryMovementType type) =>
        type is InventoryMovementType.InitialBalance
            or InventoryMovementType.PurchaseIn
            or InventoryMovementType.SaleCancellation
            or InventoryMovementType.AdjustmentIncrease
            or InventoryMovementType.CustomerReturn;

    /// <summary>
    /// Whether the movement is a manual adjustment and therefore requires an adjustment reason.
    /// </summary>
    /// <param name="type">Movement type to check.</param>
    /// <returns>True when the movement is a positive or negative manual adjustment.</returns>
    public static bool IsAdjustment(this InventoryMovementType type) =>
        type is InventoryMovementType.AdjustmentIncrease or InventoryMovementType.AdjustmentDecrease;
}
