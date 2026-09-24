using StockFlow.Domain;

namespace StockFlow.Application.Inventory.Commands;

/// <summary>
/// Command that applies a manual stock adjustment to a product.
/// </summary>
/// <param name="ProductId">Identifier of the product to adjust.</param>
/// <param name="Type">Movement type; must be a positive or negative adjustment.</param>
/// <param name="Quantity">Positive moved quantity, in base units.</param>
/// <param name="Reason">Adjustment reason; required for adjustments.</param>
/// <param name="Note">Optional note; required when the reason is "other".</param>
public record AdjustStockCommand(
    Guid ProductId,
    InventoryMovementType Type,
    decimal Quantity,
    InventoryAdjustmentReason? Reason,
    string? Note);
