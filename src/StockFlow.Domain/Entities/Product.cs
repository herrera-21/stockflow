using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

/// <summary>
/// Inventory product aggregate. Stock is intentionally not editable through <see cref="Update"/>:
/// by the inventory business rule it only changes through inventory movements (Phase 3).
/// </summary>
public class Product
{
    // EF Core materialization constructor.
    private Product()
    {
    }

    private Product(
        Guid id,
        string sku,
        string name,
        string? description,
        string category,
        decimal purchasePrice,
        decimal salePrice,
        decimal taxRate,
        int currentStock,
        int minimumStock,
        bool isActive)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Description = description;
        Category = category;
        PurchasePrice = purchasePrice;
        SalePrice = salePrice;
        TaxRate = taxRate;
        CurrentStock = currentStock;
        MinimumStock = minimumStock;
        IsActive = isActive;
    }

    /// <summary>Unique identifier (UUID v7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Stock keeping unit; unique across products.</summary>
    public string Sku { get; private set; } = null!;

    /// <summary>Display name.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Optional free-form description.</summary>
    public string? Description { get; private set; }

    /// <summary>Product category.</summary>
    public string Category { get; private set; } = null!;

    /// <summary>Price paid to the supplier.</summary>
    public decimal PurchasePrice { get; private set; }

    /// <summary>Price charged to the customer.</summary>
    public decimal SalePrice { get; private set; }

    /// <summary>Tax rate as a percentage (0-100), not a monetary amount.</summary>
    public decimal TaxRate { get; private set; }

    /// <summary>Quantity currently in stock.</summary>
    public int CurrentStock { get; private set; }

    /// <summary>Stock level at or below which the product is considered low.</summary>
    public int MinimumStock { get; private set; }

    /// <summary>Whether the product is active (soft-delete flag).</summary>
    public bool IsActive { get; private set; }

    /// <summary>True when the product is at or below its minimum stock level.</summary>
    public bool HasLowStock => CurrentStock <= MinimumStock;

    /// <summary>
    /// Creates a new active product with the given opening stock.
    /// </summary>
    /// <param name="sku">Stock keeping unit; must not be blank.</param>
    /// <param name="name">Display name; must not be blank.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="category">Category; must not be blank.</param>
    /// <param name="purchasePrice">Supplier price; cannot be negative.</param>
    /// <param name="salePrice">Customer price; cannot be negative.</param>
    /// <param name="taxRate">Tax percentage (0-100).</param>
    /// <param name="initialStock">Opening balance; cannot be negative.</param>
    /// <param name="minimumStock">Low-stock threshold; cannot be negative.</param>
    /// <returns>The created product.</returns>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public static Product Create(
        string sku,
        string name,
        string? description,
        string category,
        decimal purchasePrice,
        decimal salePrice,
        decimal taxRate,
        int initialStock,
        int minimumStock)
    {
        ValidateEditableData(sku, name, category, purchasePrice, salePrice, taxRate, minimumStock);
        ValidateStock(initialStock, nameof(initialStock));

        // The initial quantity is the opening balance; from then on stock only moves
        // through inventory movements.
        return new Product(
            Guid.CreateVersion7(),
            sku.Trim(),
            name.Trim(),
            Normalize(description),
            category.Trim(),
            purchasePrice,
            salePrice,
            taxRate,
            initialStock,
            minimumStock,
            isActive: true);
    }

    /// <summary>
    /// Updates the editable data. <see cref="CurrentStock"/> is deliberately excluded.
    /// </summary>
    /// <param name="sku">Stock keeping unit; must not be blank.</param>
    /// <param name="name">Display name; must not be blank.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="category">Category; must not be blank.</param>
    /// <param name="purchasePrice">Supplier price; cannot be negative.</param>
    /// <param name="salePrice">Customer price; cannot be negative.</param>
    /// <param name="taxRate">Tax percentage (0-100).</param>
    /// <param name="minimumStock">Low-stock threshold; cannot be negative.</param>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public void Update(
        string sku,
        string name,
        string? description,
        string category,
        decimal purchasePrice,
        decimal salePrice,
        decimal taxRate,
        int minimumStock)
    {
        ValidateEditableData(sku, name, category, purchasePrice, salePrice, taxRate, minimumStock);

        Sku = sku.Trim();
        Name = name.Trim();
        Description = Normalize(description);
        Category = category.Trim();
        PurchasePrice = purchasePrice;
        SalePrice = salePrice;
        TaxRate = taxRate;
        MinimumStock = minimumStock;
    }

    /// <summary>
    /// Deactivates the product. This is a soft delete: products are never removed physically.
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reactivates a previously deactivated product.</summary>
    public void Activate() => IsActive = true;

    // Validates the editable fields shared by Create and Update.
    private static void ValidateEditableData(
        string sku,
        string name,
        string category,
        decimal purchasePrice,
        decimal salePrice,
        decimal taxRate,
        int minimumStock)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("SKU is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new DomainException("Category is required.");
        }

        if (purchasePrice < 0)
        {
            throw new DomainException("Purchase price cannot be negative.");
        }

        if (salePrice < 0)
        {
            throw new DomainException("Sale price cannot be negative.");
        }

        if (taxRate is < 0 or > 100)
        {
            throw new DomainException("Tax rate must be between 0 and 100.");
        }

        ValidateStock(minimumStock, nameof(minimumStock));
    }

    // Ensures a stock value is not negative.
    private static void ValidateStock(int value, string paramName)
    {
        if (value < 0)
        {
            throw new DomainException($"{paramName} cannot be negative.");
        }
    }

    // Trims the value and turns blank strings into null.
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
