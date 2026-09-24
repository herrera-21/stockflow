using StockFlow.Domain.Entities;

namespace StockFlow.Application.Inventory;

/// <summary>
/// Maps inventory movement domain entities to their DTO representation.
/// </summary>
internal static class InventoryMovementMapping
{
    /// <summary>
    /// Projects an <see cref="InventoryMovement"/> into an <see cref="InventoryMovementDto"/>.
    /// </summary>
    /// <param name="movement">Movement to project.</param>
    /// <returns>The projected DTO.</returns>
    public static InventoryMovementDto ToDto(this InventoryMovement movement) => new(
        movement.Id,
        movement.ProductId,
        movement.Type,
        movement.Quantity,
        movement.StockBefore,
        movement.StockAfter,
        movement.BaseUnit,
        movement.Reason,
        movement.Note,
        movement.UserName,
        movement.OccurredAt,
        movement.ReferenceType,
        movement.ReferenceId);
}
