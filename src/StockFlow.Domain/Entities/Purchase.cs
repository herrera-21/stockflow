using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

/// <summary>
/// Purchase order aggregate. It starts as a draft that can be edited, is confirmed once the lines are
/// final, and is received into stock afterwards. A purchase is only editable while it is a draft;
/// once confirmed it can just be received or cancelled.
/// </summary>
public class Purchase
{
    // Ordered lines, in insertion order.
    private readonly List<PurchaseLine> _lines = [];

    // EF Core materialization constructor.
    private Purchase()
    {
    }

    private Purchase(
        Guid id,
        Guid supplierId,
        string number,
        DateTimeOffset orderDate,
        PurchaseStatus status,
        string? note,
        string createdByUserId,
        string createdByUserName)
    {
        Id = id;
        SupplierId = supplierId;
        Number = number;
        OrderDate = orderDate;
        Status = status;
        Note = note;
        CreatedByUserId = createdByUserId;
        CreatedByUserName = createdByUserName;
    }

    /// <summary>Unique identifier (UUID v7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Identifier of the supplier the order is placed with.</summary>
    public Guid SupplierId { get; private set; }

    /// <summary>Supplier the order is placed with.</summary>
    public Supplier? Supplier { get; private set; }

    /// <summary>Human-readable order number, for example <c>OC-000001</c>; unique across purchases.</summary>
    public string Number { get; private set; } = null!;

    /// <summary>Instant the order was placed.</summary>
    public DateTimeOffset OrderDate { get; private set; }

    /// <summary>Current lifecycle state.</summary>
    public PurchaseStatus Status { get; private set; }

    /// <summary>Optional free-form note.</summary>
    public string? Note { get; private set; }

    /// <summary>Identifier of the user that created the order.</summary>
    public string CreatedByUserId { get; private set; } = null!;

    /// <summary>Display name (email) of the user that created the order, snapshotted.</summary>
    public string CreatedByUserName { get; private set; } = null!;

    /// <summary>Ordered lines, in insertion order.</summary>
    public IReadOnlyList<PurchaseLine> Lines => _lines;

    /// <summary>Order total: the sum of the line subtotals.</summary>
    public decimal Total => _lines.Sum(line => line.Subtotal);

    /// <summary>
    /// Creates a new purchase order in <see cref="PurchaseStatus.Draft"/>. The number is assigned by
    /// the caller from the purchase order sequence, already formatted by <see cref="PurchaseNumber"/>.
    /// </summary>
    /// <param name="supplierId">Supplier identifier; cannot be empty.</param>
    /// <param name="number">Formatted order number; must not be blank.</param>
    /// <param name="orderDate">Instant the order is placed.</param>
    /// <param name="note">Optional free-form note.</param>
    /// <param name="createdByUserId">Identifier of the creating user; must not be blank.</param>
    /// <param name="createdByUserName">Display name of the creating user; must not be blank.</param>
    /// <returns>The created draft order.</returns>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public static Purchase Create(
        Guid supplierId,
        string number,
        DateTimeOffset orderDate,
        string? note,
        string createdByUserId,
        string createdByUserName)
    {
        if (supplierId == Guid.Empty)
        {
            throw new DomainException("Supplier is required.");
        }

        if (string.IsNullOrWhiteSpace(number))
        {
            throw new DomainException("Order number is required.");
        }

        if (string.IsNullOrWhiteSpace(createdByUserId) || string.IsNullOrWhiteSpace(createdByUserName))
        {
            throw new DomainException("The creating user is required.");
        }

        return new Purchase(
            Guid.CreateVersion7(),
            supplierId,
            number.Trim(),
            orderDate,
            PurchaseStatus.Draft,
            Normalize(note),
            createdByUserId,
            createdByUserName);
    }

    /// <summary>
    /// Adds a line, snapshotting the product's purchase unit, base unit and factor. Only a draft can
    /// be edited, and a product cannot appear twice in the same order.
    /// </summary>
    /// <param name="product">Ordered product; its current units are captured on the line.</param>
    /// <param name="quantity">Quantity in the product's purchase unit; positive and whole for countable units.</param>
    /// <param name="purchaseUnitCost">Cost of one purchase unit; cannot be negative.</param>
    /// <returns>The created line.</returns>
    /// <exception cref="DomainException">When the order is not a draft, the product repeats, or the line data is invalid.</exception>
    public PurchaseLine AddLine(Product product, decimal quantity, decimal purchaseUnitCost)
    {
        EnsureDraft();

        if (product is null)
        {
            throw new DomainException("Product is required.");
        }

        if (_lines.Any(line => line.ProductId == product.Id))
        {
            throw new DomainException("A product cannot be added twice to the same order.");
        }

        var line = PurchaseLine.Create(Id, product, quantity, purchaseUnitCost);
        _lines.Add(line);

        return line;
    }

    /// <summary>
    /// Updates the quantity and cost of an existing line, keeping its unit snapshot. Only a draft can
    /// be edited.
    /// </summary>
    /// <param name="lineId">Identifier of the line to update; must belong to this order.</param>
    /// <param name="quantity">New quantity in the line's purchase unit; positive and whole for countable units.</param>
    /// <param name="purchaseUnitCost">New cost of one purchase unit; cannot be negative.</param>
    /// <exception cref="DomainException">When the order is not a draft, the line is unknown, or the values are invalid.</exception>
    public void UpdateLine(Guid lineId, decimal quantity, decimal purchaseUnitCost)
    {
        EnsureDraft();

        var line = _lines.FirstOrDefault(candidate => candidate.Id == lineId)
            ?? throw new DomainException("The line does not belong to this order.");

        line.Update(quantity, purchaseUnitCost);
    }

    /// <summary>
    /// Removes a line. Only a draft can be edited; removing an unknown line does nothing.
    /// </summary>
    /// <param name="lineId">Identifier of the line to remove.</param>
    /// <exception cref="DomainException">When the order is not a draft.</exception>
    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();

        var line = _lines.FirstOrDefault(candidate => candidate.Id == lineId);
        if (line is not null)
        {
            _lines.Remove(line);
        }
    }

    /// <summary>
    /// Confirms a draft order. An order without lines cannot be confirmed.
    /// </summary>
    /// <exception cref="DomainException">When the order is not a draft or has no lines.</exception>
    public void Confirm()
    {
        if (Status != PurchaseStatus.Draft)
        {
            throw new DomainException("Only a draft order can be confirmed.");
        }

        if (_lines.Count == 0)
        {
            throw new DomainException("An order without lines cannot be confirmed.");
        }

        Status = PurchaseStatus.Confirmed;
    }

    /// <summary>
    /// Receives a confirmed order. Booking the stock movements that correspond to each line is Phase
    /// 4.2; for now this only advances the state.
    /// </summary>
    /// <exception cref="DomainException">When the order is not confirmed.</exception>
    public void Receive()
    {
        if (Status != PurchaseStatus.Confirmed)
        {
            throw new DomainException("Only a confirmed order can be received.");
        }

        Status = PurchaseStatus.Received;
    }

    /// <summary>
    /// Cancels the order. A received order cannot be cancelled, and an already cancelled order cannot
    /// be cancelled again.
    /// </summary>
    /// <exception cref="DomainException">When the order is received or already cancelled.</exception>
    public void Cancel()
    {
        if (Status == PurchaseStatus.Received)
        {
            throw new DomainException("A received order cannot be cancelled.");
        }

        if (Status == PurchaseStatus.Cancelled)
        {
            throw new DomainException("The order is already cancelled.");
        }

        Status = PurchaseStatus.Cancelled;
    }

    // Every line edit requires the order to still be a draft.
    private void EnsureDraft()
    {
        if (Status != PurchaseStatus.Draft)
        {
            throw new DomainException("Only a draft order can be edited.");
        }
    }

    // Trims the value and turns blank strings into null.
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
