using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

/// <summary>
/// Line of a <see cref="Purchase"/>. It snapshots the product's purchase unit, base unit and
/// conversion factor when the line is created, so later changes to the product do not rewrite the
/// historical order. Quantities are expressed in the snapshotted purchase unit.
/// </summary>
public class PurchaseLine
{
    // EF Core materialization constructor.
    private PurchaseLine()
    {
    }

    private PurchaseLine(
        Guid id,
        Guid purchaseId,
        Guid productId,
        decimal quantity,
        decimal purchaseUnitCost,
        UnitOfMeasure purchaseUnit,
        UnitOfMeasure baseUnit,
        decimal purchaseUnitFactor)
    {
        Id = id;
        PurchaseId = purchaseId;
        ProductId = productId;
        Quantity = quantity;
        PurchaseUnitCost = purchaseUnitCost;
        PurchaseUnit = purchaseUnit;
        BaseUnit = baseUnit;
        PurchaseUnitFactor = purchaseUnitFactor;
        Subtotal = CalculateSubtotal(quantity, purchaseUnitCost);
    }

    /// <summary>Unique identifier (UUID v7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Identifier of the purchase order this line belongs to.</summary>
    public Guid PurchaseId { get; private set; }

    /// <summary>Identifier of the ordered product.</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Ordered quantity, expressed in the snapshotted <see cref="PurchaseUnit"/>.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Cost of one purchase unit, snapshotted from the agreed product price.</summary>
    public decimal PurchaseUnitCost { get; private set; }

    /// <summary>Purchase unit at the time the line was created.</summary>
    public UnitOfMeasure PurchaseUnit { get; private set; }

    /// <summary>Base unit at the time the line was created; stock is received in this unit.</summary>
    public UnitOfMeasure BaseUnit { get; private set; }

    /// <summary>Base units per purchase unit at the time the line was created.</summary>
    public decimal PurchaseUnitFactor { get; private set; }

    /// <summary>Line amount: <see cref="Quantity"/> times <see cref="PurchaseUnitCost"/>, rounded to 2 decimals.</summary>
    public decimal Subtotal { get; private set; }

    // Creates a line and snapshots the product units. Only Purchase calls this, which is why the
    // snapshot cannot diverge from the product at creation time.
    internal static PurchaseLine Create(Guid purchaseId, Product product, decimal quantity, decimal purchaseUnitCost)
    {
        if (purchaseId == Guid.Empty)
        {
            throw new DomainException("Purchase is required.");
        }

        if (product is null)
        {
            throw new DomainException("Product is required.");
        }

        Validate(quantity, purchaseUnitCost, product.PurchaseUnitFactor, product.PurchaseUnit);

        return new PurchaseLine(
            Guid.CreateVersion7(),
            purchaseId,
            product.Id,
            quantity,
            purchaseUnitCost,
            product.PurchaseUnit,
            product.BaseUnit,
            product.PurchaseUnitFactor);
    }

    // Updates quantity and cost in place, keeping the original unit snapshot. Called by Purchase
    // only while the order is a draft.
    internal void Update(decimal quantity, decimal purchaseUnitCost)
    {
        ValidateQuantity(quantity, PurchaseUnit);
        ValidatePurchaseUnitCost(purchaseUnitCost);

        Quantity = quantity;
        PurchaseUnitCost = purchaseUnitCost;
        Subtotal = CalculateSubtotal(quantity, purchaseUnitCost);
    }

    // Validates quantity and cost, and the snapshotted factor.
    private static void Validate(decimal quantity, decimal purchaseUnitCost, decimal purchaseUnitFactor, UnitOfMeasure purchaseUnit)
    {
        ValidateQuantity(quantity, purchaseUnit);
        ValidatePurchaseUnitCost(purchaseUnitCost);

        if (purchaseUnitFactor <= 0)
        {
            throw new DomainException("The purchase unit factor must be greater than zero.");
        }

        if (!Enum.IsDefined(purchaseUnit))
        {
            throw new DomainException("Purchase unit is not valid.");
        }
    }

    // The quantity must be positive and whole when the purchase unit is countable.
    private static void ValidateQuantity(decimal quantity, UnitOfMeasure purchaseUnit)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Line quantity must be greater than zero.");
        }

        if (!purchaseUnit.IsValidQuantity(quantity))
        {
            throw new DomainException("Line quantity must be a whole number for this purchase unit.");
        }
    }

    // The cost per purchase unit cannot be negative.
    private static void ValidatePurchaseUnitCost(decimal purchaseUnitCost)
    {
        if (purchaseUnitCost < 0)
        {
            throw new DomainException("Line cost cannot be negative.");
        }
    }

    // Line amount rounded to two decimals, away from zero.
    private static decimal CalculateSubtotal(decimal quantity, decimal purchaseUnitCost) =>
        Math.Round(quantity * purchaseUnitCost, 2, MidpointRounding.AwayFromZero);
}
