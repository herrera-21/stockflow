using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

/// <summary>
/// Join entity linking a <see cref="Product"/> to a <see cref="Supplier"/>. It carries the
/// supplier-specific data: the supplier's own reference (SKU) and the agreed purchase price. The
/// preferred flag is managed by <see cref="Product"/> so that at most one supplier is preferred.
/// </summary>
public class ProductSupplier
{
    // EF Core materialization constructor.
    private ProductSupplier()
    {
    }

    private ProductSupplier(
        Guid productId,
        Guid supplierId,
        string? supplierSku,
        decimal? purchasePrice,
        bool isPreferred)
    {
        ProductId = productId;
        SupplierId = supplierId;
        SupplierSku = supplierSku;
        PurchasePrice = purchasePrice;
        IsPreferred = isPreferred;
    }

    /// <summary>Identifier of the associated product (part of the composite key).</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Identifier of the associated supplier (part of the composite key).</summary>
    public Guid SupplierId { get; private set; }

    /// <summary>Optional reference the supplier uses for this product.</summary>
    public string? SupplierSku { get; private set; }

    /// <summary>Optional purchase price agreed with this supplier.</summary>
    public decimal? PurchasePrice { get; private set; }

    /// <summary>Whether this supplier is the preferred one for the product.</summary>
    public bool IsPreferred { get; private set; }

    /// <summary>
    /// Creates a new association between a product and a supplier.
    /// </summary>
    /// <param name="productId">Product identifier; cannot be empty.</param>
    /// <param name="supplierId">Supplier identifier; cannot be empty.</param>
    /// <param name="supplierSku">Optional supplier reference.</param>
    /// <param name="purchasePrice">Optional agreed purchase price; cannot be negative.</param>
    /// <returns>The created association, not preferred by default.</returns>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public static ProductSupplier Create(
        Guid productId,
        Guid supplierId,
        string? supplierSku,
        decimal? purchasePrice)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product is required.");
        }

        if (supplierId == Guid.Empty)
        {
            throw new DomainException("Supplier is required.");
        }

        ValidatePurchasePrice(purchasePrice);

        return new ProductSupplier(productId, supplierId, Normalize(supplierSku), purchasePrice, isPreferred: false);
    }

    /// <summary>
    /// Updates the supplier-specific data of the association. The preferred flag is not changed
    /// here; it is owned by <see cref="Product"/>.
    /// </summary>
    /// <param name="supplierSku">Optional supplier reference.</param>
    /// <param name="purchasePrice">Optional agreed purchase price; cannot be negative.</param>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public void Update(string? supplierSku, decimal? purchasePrice)
    {
        ValidatePurchasePrice(purchasePrice);

        SupplierSku = Normalize(supplierSku);
        PurchasePrice = purchasePrice;
    }

    // Sets the preferred flag. Called by Product, which keeps at most one preferred supplier.
    internal void SetPreferred(bool value) => IsPreferred = value;

    // Ensures the optional purchase price is not negative.
    private static void ValidatePurchasePrice(decimal? purchasePrice)
    {
        if (purchasePrice < 0)
        {
            throw new DomainException("Purchase price cannot be negative.");
        }
    }

    // Trims the value and turns blank strings into null.
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
