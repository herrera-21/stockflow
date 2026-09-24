using StockFlow.Domain;

namespace StockFlow.Application.Inventory;

/// <summary>
/// Inventory movement data transferred across the Application boundary.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="ProductId">Identifier of the product whose stock changed.</param>
/// <param name="Type">Movement type, which also gives the direction.</param>
/// <param name="Quantity">Moved quantity as a positive magnitude, in base units.</param>
/// <param name="StockBefore">Stock in base units before the movement.</param>
/// <param name="StockAfter">Stock in base units after the movement.</param>
/// <param name="BaseUnit">Base unit the stock was expressed in.</param>
/// <param name="Reason">Reason for a manual adjustment; null for system-driven movements.</param>
/// <param name="Note">Optional note; required when the reason is "other".</param>
/// <param name="UserName">Display name of the user that produced the movement.</param>
/// <param name="OccurredAt">Instant the movement was recorded.</param>
/// <param name="ReferenceType">Kind of referenced document; null when not tied to one.</param>
/// <param name="ReferenceId">Referenced document identifier; null when not tied to one.</param>
public record InventoryMovementDto(
    Guid Id,
    Guid ProductId,
    InventoryMovementType Type,
    decimal Quantity,
    decimal StockBefore,
    decimal StockAfter,
    UnitOfMeasure BaseUnit,
    InventoryAdjustmentReason? Reason,
    string? Note,
    string UserName,
    DateTimeOffset OccurredAt,
    InventoryReferenceType? ReferenceType,
    Guid? ReferenceId);
