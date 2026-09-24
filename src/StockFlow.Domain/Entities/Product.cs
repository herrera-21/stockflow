using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

/// <summary>
/// Inventory product aggregate. Stock is intentionally not editable through <see cref="Update"/>:
/// by the inventory business rule it only changes through inventory movements (Phase 3).
/// </summary>
public class Product
{
    // Suppliers that offer this product (many-to-many through ProductSupplier).
    private readonly List<ProductSupplier> _suppliers = [];

    // EF Core materialization constructor.
    private Product()
    {
    }

    private Product(
        Guid id,
        string sku,
        string name,
        string? description,
        Guid categoryId,
        UnitOfMeasure baseUnit,
        UnitOfMeasure purchaseUnit,
        decimal purchaseUnitFactor,
        decimal purchasePrice,
        decimal salePrice,
        decimal taxRate,
        decimal currentStock,
        decimal minimumStock,
        bool isActive)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Description = description;
        CategoryId = categoryId;
        BaseUnit = baseUnit;
        PurchaseUnit = purchaseUnit;
        PurchaseUnitFactor = purchaseUnitFactor;
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

    /// <summary>Identifier of the product category.</summary>
    public Guid CategoryId { get; private set; }

    /// <summary>Category this product belongs to.</summary>
    public Category? Category { get; private set; }

    /// <summary>Unit in which stock is kept and the product is sold.</summary>
    public UnitOfMeasure BaseUnit { get; private set; }

    /// <summary>Unit in which the product is bought from suppliers.</summary>
    public UnitOfMeasure PurchaseUnit { get; private set; }

    /// <summary>
    /// How many base units one purchase unit contains (for example 24 when a box holds 24 units).
    /// Always 1 when both units are the same.
    /// </summary>
    public decimal PurchaseUnitFactor { get; private set; }

    /// <summary>Price paid to the supplier per purchase unit.</summary>
    public decimal PurchasePrice { get; private set; }

    /// <summary>Price charged to the customer per base unit.</summary>
    public decimal SalePrice { get; private set; }

    /// <summary>Tax rate as a percentage (0-100), not a monetary amount.</summary>
    public decimal TaxRate { get; private set; }

    /// <summary>Quantity currently in stock, in base units.</summary>
    public decimal CurrentStock { get; private set; }

    /// <summary>Stock level (in base units) at or below which the product is considered low.</summary>
    public decimal MinimumStock { get; private set; }

    /// <summary>Whether the product is active (soft-delete flag).</summary>
    public bool IsActive { get; private set; }

    /// <summary>True when the product is at or below its minimum stock level.</summary>
    public bool HasLowStock => CurrentStock <= MinimumStock;

    /// <summary>Cost of one base unit: the purchase price divided by the purchase unit factor.</summary>
    public decimal UnitCost => PurchasePrice / PurchaseUnitFactor;

    /// <summary>Suppliers that offer this product.</summary>
    public IReadOnlyList<ProductSupplier> Suppliers => _suppliers;

    /// <summary>
    /// Creates a new active product with the given opening stock.
    /// </summary>
    /// <param name="sku">Stock keeping unit; must not be blank.</param>
    /// <param name="name">Display name; must not be blank.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="categoryId">Category identifier; must not be empty.</param>
    /// <param name="baseUnit">Unit for stock and sale price.</param>
    /// <param name="purchaseUnit">Unit used when buying from suppliers.</param>
    /// <param name="purchaseUnitFactor">
    /// Base units per purchase unit; positive, whole for countable base units and 1 when both units
    /// match.
    /// </param>
    /// <param name="purchasePrice">Supplier price per purchase unit; cannot be negative.</param>
    /// <param name="salePrice">Customer price per base unit; cannot be negative.</param>
    /// <param name="taxRate">Tax percentage (0-100).</param>
    /// <param name="initialStock">Opening balance in base units; cannot be negative.</param>
    /// <param name="minimumStock">Low-stock threshold in base units; cannot be negative.</param>
    /// <returns>The created product.</returns>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public static Product Create(
        string sku,
        string name,
        string? description,
        Guid categoryId,
        UnitOfMeasure baseUnit,
        UnitOfMeasure purchaseUnit,
        decimal purchaseUnitFactor,
        decimal purchasePrice,
        decimal salePrice,
        decimal taxRate,
        decimal initialStock,
        decimal minimumStock)
    {
        ValidateEditableData(
            sku, name, categoryId, baseUnit, purchaseUnit, purchaseUnitFactor,
            purchasePrice, salePrice, taxRate, minimumStock);
        ValidateStock(initialStock, baseUnit, nameof(initialStock));

        // The initial quantity is the opening balance; from then on stock only moves
        // through inventory movements.
        return new Product(
            Guid.CreateVersion7(),
            sku.Trim(),
            name.Trim(),
            Normalize(description),
            categoryId,
            baseUnit,
            purchaseUnit,
            purchaseUnitFactor,
            purchasePrice,
            salePrice,
            taxRate,
            initialStock,
            minimumStock,
            isActive: true);
    }

    /// <summary>
    /// Updates the editable data. <see cref="CurrentStock"/> is deliberately excluded, and the base
    /// unit can only change while there is no stock.
    /// </summary>
    /// <param name="sku">Stock keeping unit; must not be blank.</param>
    /// <param name="name">Display name; must not be blank.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="categoryId">Category identifier; must not be empty.</param>
    /// <param name="baseUnit">Unit for stock and sale price.</param>
    /// <param name="purchaseUnit">Unit used when buying from suppliers.</param>
    /// <param name="purchaseUnitFactor">
    /// Base units per purchase unit; positive, whole for countable base units and 1 when both units
    /// match.
    /// </param>
    /// <param name="purchasePrice">Supplier price per purchase unit; cannot be negative.</param>
    /// <param name="salePrice">Customer price per base unit; cannot be negative.</param>
    /// <param name="taxRate">Tax percentage (0-100).</param>
    /// <param name="minimumStock">Low-stock threshold in base units; cannot be negative.</param>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public void Update(
        string sku,
        string name,
        string? description,
        Guid categoryId,
        UnitOfMeasure baseUnit,
        UnitOfMeasure purchaseUnit,
        decimal purchaseUnitFactor,
        decimal purchasePrice,
        decimal salePrice,
        decimal taxRate,
        decimal minimumStock)
    {
        ValidateEditableData(
            sku, name, categoryId, baseUnit, purchaseUnit, purchaseUnitFactor,
            purchasePrice, salePrice, taxRate, minimumStock);

        if (!CanChangeBaseUnitTo(baseUnit))
        {
            throw new DomainException("The base unit cannot change while the product has stock.");
        }

        Sku = sku.Trim();
        Name = name.Trim();
        Description = Normalize(description);
        CategoryId = categoryId;
        BaseUnit = baseUnit;
        PurchaseUnit = purchaseUnit;
        PurchaseUnitFactor = purchaseUnitFactor;
        PurchasePrice = purchasePrice;
        SalePrice = salePrice;
        TaxRate = taxRate;
        MinimumStock = minimumStock;
    }

    /// <summary>
    /// Whether the base unit may be set to the given value. Changing it with stock on hand would
    /// silently reinterpret the existing quantity (10 units would become 10 pounds), so a change is
    /// only allowed at zero stock.
    /// </summary>
    /// <param name="baseUnit">Candidate base unit.</param>
    /// <returns>True when the unit is unchanged or the product has no stock.</returns>
    public bool CanChangeBaseUnitTo(UnitOfMeasure baseUnit) =>
        baseUnit == BaseUnit || CurrentStock == 0;

    /// <summary>
    /// Deactivates the product. This is a soft delete: products are never removed physically.
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reactivates a previously deactivated product.</summary>
    public void Activate() => IsActive = true;

    /// <summary>
    /// Adds a supplier to this product or updates the data of an existing association.
    /// </summary>
    /// <param name="supplierId">Supplier identifier; cannot be empty.</param>
    /// <param name="supplierSku">Optional supplier reference.</param>
    /// <param name="purchasePrice">Optional agreed purchase price; cannot be negative.</param>
    /// <param name="isPreferred">Whether this supplier becomes the preferred one for the product.</param>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public void AssignSupplier(Guid supplierId, string? supplierSku, decimal? purchasePrice, bool isPreferred)
    {
        var association = _suppliers.FirstOrDefault(s => s.SupplierId == supplierId);

        if (association is null)
        {
            association = ProductSupplier.Create(Id, supplierId, supplierSku, purchasePrice);
            _suppliers.Add(association);
        }
        else
        {
            association.Update(supplierSku, purchasePrice);
        }

        if (isPreferred)
        {
            SetPreferredSupplier(supplierId);
        }
        else
        {
            association.SetPreferred(false);
        }
    }

    /// <summary>
    /// Marks the given supplier as the preferred one, clearing the flag from the others. A product
    /// has at most one preferred supplier.
    /// </summary>
    /// <param name="supplierId">Supplier to mark as preferred; must be associated with the product.</param>
    /// <exception cref="DomainException">When the supplier is not associated with the product.</exception>
    public void SetPreferredSupplier(Guid supplierId)
    {
        if (_suppliers.All(s => s.SupplierId != supplierId))
        {
            throw new DomainException("The supplier is not associated with this product.");
        }

        foreach (var association in _suppliers)
        {
            association.SetPreferred(association.SupplierId == supplierId);
        }
    }

    /// <summary>
    /// Removes a supplier from this product. Does nothing when the supplier is not associated.
    /// </summary>
    /// <param name="supplierId">Supplier identifier to remove.</param>
    public void RemoveSupplier(Guid supplierId)
    {
        var association = _suppliers.FirstOrDefault(s => s.SupplierId == supplierId);
        if (association is not null)
        {
            _suppliers.Remove(association);
        }
    }

    // Validates the editable fields shared by Create and Update.
    private static void ValidateEditableData(
        string sku,
        string name,
        Guid categoryId,
        UnitOfMeasure baseUnit,
        UnitOfMeasure purchaseUnit,
        decimal purchaseUnitFactor,
        decimal purchasePrice,
        decimal salePrice,
        decimal taxRate,
        decimal minimumStock)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("SKU is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name is required.");
        }

        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category is required.");
        }

        if (!Enum.IsDefined(baseUnit) || !Enum.IsDefined(purchaseUnit))
        {
            throw new DomainException("Unit of measure is not valid.");
        }

        ValidatePurchaseUnitFactor(baseUnit, purchaseUnit, purchaseUnitFactor);

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

        ValidateStock(minimumStock, baseUnit, nameof(minimumStock));
    }

    // Ensures the conversion factor is positive, is 1 for identical units and holds a whole number
    // of countable base units (a box cannot hold 2.5 pieces).
    private static void ValidatePurchaseUnitFactor(
        UnitOfMeasure baseUnit,
        UnitOfMeasure purchaseUnit,
        decimal purchaseUnitFactor)
    {
        if (purchaseUnitFactor <= 0)
        {
            throw new DomainException("Purchase unit factor must be greater than zero.");
        }

        if (purchaseUnit == baseUnit && purchaseUnitFactor != 1)
        {
            throw new DomainException("Purchase unit factor must be 1 when both units are the same.");
        }

        if (!baseUnit.IsValidQuantity(purchaseUnitFactor))
        {
            throw new DomainException("Purchase unit factor must be a whole number for this base unit.");
        }
    }

    // Ensures a stock value is not negative and fits the base unit (whole for countable units).
    private static void ValidateStock(decimal value, UnitOfMeasure baseUnit, string paramName)
    {
        if (value < 0)
        {
            throw new DomainException($"{paramName} cannot be negative.");
        }

        if (!baseUnit.IsValidQuantity(value))
        {
            throw new DomainException($"{paramName} must be a whole number for this unit.");
        }
    }

    // Trims the value and turns blank strings into null.
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
