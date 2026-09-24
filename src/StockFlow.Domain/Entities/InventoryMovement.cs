using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

/// <summary>
/// Immutable record of a stock change. Every change to <see cref="Product.CurrentStock"/> goes
/// through a movement, which keeps the stock auditable: who changed it, when, why and what the
/// quantity was before and after.
/// </summary>
public class InventoryMovement
{
    // EF Core materialization constructor.
    private InventoryMovement()
    {
    }

    private InventoryMovement(
        Guid id,
        Guid productId,
        InventoryMovementType type,
        decimal quantity,
        decimal stockBefore,
        decimal stockAfter,
        UnitOfMeasure baseUnit,
        InventoryAdjustmentReason? reason,
        string? note,
        string userId,
        string userName,
        DateTimeOffset occurredAt,
        InventoryReferenceType? referenceType,
        Guid? referenceId)
    {
        Id = id;
        ProductId = productId;
        Type = type;
        Quantity = quantity;
        StockBefore = stockBefore;
        StockAfter = stockAfter;
        BaseUnit = baseUnit;
        Reason = reason;
        Note = note;
        UserId = userId;
        UserName = userName;
        OccurredAt = occurredAt;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
    }

    /// <summary>Unique identifier (UUID v7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Identifier of the product whose stock changed.</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Product whose stock changed.</summary>
    public Product? Product { get; private set; }

    /// <summary>Type of movement, which also gives the direction (inbound or outbound).</summary>
    public InventoryMovementType Type { get; private set; }

    /// <summary>Moved quantity as a positive magnitude, in base units.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Stock in base units before the movement was applied.</summary>
    public decimal StockBefore { get; private set; }

    /// <summary>Stock in base units after the movement was applied.</summary>
    public decimal StockAfter { get; private set; }

    /// <summary>Base unit the stock was expressed in when the movement happened.</summary>
    public UnitOfMeasure BaseUnit { get; private set; }

    /// <summary>Reason for a manual adjustment; null for system-driven movements.</summary>
    public InventoryAdjustmentReason? Reason { get; private set; }

    /// <summary>Optional free-form note; required when the reason is <see cref="InventoryAdjustmentReason.Other"/>.</summary>
    public string? Note { get; private set; }

    /// <summary>Identifier of the user that produced the movement.</summary>
    public string UserId { get; private set; } = null!;

    /// <summary>Display name (email) of the user that produced the movement, snapshotted.</summary>
    public string UserName { get; private set; } = null!;

    /// <summary>Instant the movement was recorded, in UTC.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Kind of document the movement refers to; null when it is not tied to one.</summary>
    public InventoryReferenceType? ReferenceType { get; private set; }

    /// <summary>Identifier of the referenced document; null when it is not tied to one.</summary>
    public Guid? ReferenceId { get; private set; }

    /// <summary>
    /// Creates a movement after checking its invariants: a defined type, a positive quantity that
    /// fits the unit, non-negative stock values consistent with the direction, an adjustment reason
    /// for manual adjustments and a note when that reason is <see cref="InventoryAdjustmentReason.Other"/>.
    /// </summary>
    /// <param name="productId">Product identifier; cannot be empty.</param>
    /// <param name="type">Movement type; must be defined.</param>
    /// <param name="quantity">Moved quantity as a positive magnitude.</param>
    /// <param name="stockBefore">Stock before the movement; cannot be negative.</param>
    /// <param name="stockAfter">Stock after the movement; cannot be negative.</param>
    /// <param name="baseUnit">Base unit the stock is expressed in.</param>
    /// <param name="reason">Adjustment reason; required for manual adjustments.</param>
    /// <param name="note">Optional note; required when the reason is "other".</param>
    /// <param name="userId">User that produced the movement.</param>
    /// <param name="userName">Display name of that user.</param>
    /// <param name="occurredAt">Instant the movement happened.</param>
    /// <param name="referenceType">Optional kind of referenced document.</param>
    /// <param name="referenceId">Optional referenced document identifier.</param>
    /// <returns>The created movement.</returns>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public static InventoryMovement Create(
        Guid productId,
        InventoryMovementType type,
        decimal quantity,
        decimal stockBefore,
        decimal stockAfter,
        UnitOfMeasure baseUnit,
        InventoryAdjustmentReason? reason,
        string? note,
        string userId,
        string userName,
        DateTimeOffset occurredAt,
        InventoryReferenceType? referenceType = null,
        Guid? referenceId = null)
    {
        Validate(productId, type, quantity, stockBefore, stockAfter, baseUnit, reason, note, userId, userName, referenceType);

        var expectedAfter = type.IsInbound() ? stockBefore + quantity : stockBefore - quantity;
        if (stockAfter != expectedAfter)
        {
            throw new DomainException("The resulting stock does not match the movement direction and quantity.");
        }

        return new InventoryMovement(
            Guid.CreateVersion7(),
            productId,
            type,
            quantity,
            stockBefore,
            stockAfter,
            baseUnit,
            reason,
            Normalize(note),
            userId,
            userName,
            occurredAt,
            referenceType,
            referenceId);
    }

    // Checks the invariants shared by every movement before the before/after consistency check.
    private static void Validate(
        Guid productId,
        InventoryMovementType type,
        decimal quantity,
        decimal stockBefore,
        decimal stockAfter,
        UnitOfMeasure baseUnit,
        InventoryAdjustmentReason? reason,
        string? note,
        string userId,
        string userName,
        InventoryReferenceType? referenceType)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product is required.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Movement type is not valid.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Movement quantity must be greater than zero.");
        }

        if (!baseUnit.IsValidQuantity(quantity))
        {
            throw new DomainException("Movement quantity must be a whole number for this unit.");
        }

        if (stockBefore < 0 || stockAfter < 0)
        {
            throw new DomainException("Stock cannot be negative.");
        }

        if (type.IsAdjustment())
        {
            if (reason is null)
            {
                throw new DomainException("An adjustment movement requires a reason.");
            }

            if (reason == InventoryAdjustmentReason.Other && string.IsNullOrWhiteSpace(note))
            {
                throw new DomainException("A note is required when the adjustment reason is 'other'.");
            }
        }
        else if (reason is not null)
        {
            throw new DomainException("A reason is only allowed for adjustment movements.");
        }

        if (reason is not null && !Enum.IsDefined(reason.Value))
        {
            throw new DomainException("Adjustment reason is not valid.");
        }

        if (referenceType is not null && !Enum.IsDefined(referenceType.Value))
        {
            throw new DomainException("Reference type is not valid.");
        }

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(userName))
        {
            throw new DomainException("The movement user is required.");
        }
    }

    // Trims the value and turns blank strings into null.
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
