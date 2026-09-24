using StockFlow.Domain;

namespace StockFlow.Application.Products.Commands;

/// <summary>
/// Command that updates an existing product. Current stock is not editable.
/// </summary>
/// <param name="Id">Identifier of the product to update.</param>
/// <param name="Sku">Stock keeping unit; must be unique.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Identifier of the product category.</param>
/// <param name="BaseUnit">Unit for stock and sale price; can only change while there is no stock.</param>
/// <param name="PurchaseUnit">Unit used when buying from suppliers.</param>
/// <param name="PurchaseUnitFactor">Base units contained in one purchase unit.</param>
/// <param name="PurchasePrice">Supplier price per purchase unit.</param>
/// <param name="SalePrice">Customer price per base unit.</param>
/// <param name="TaxRate">Tax percentage (0-100).</param>
/// <param name="MinimumStock">Low-stock threshold, in base units.</param>
public record UpdateProductCommand(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    Guid CategoryId,
    UnitOfMeasure BaseUnit,
    UnitOfMeasure PurchaseUnit,
    decimal PurchaseUnitFactor,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal TaxRate,
    decimal MinimumStock);
