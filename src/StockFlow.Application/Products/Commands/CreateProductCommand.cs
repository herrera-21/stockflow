using StockFlow.Domain;

namespace StockFlow.Application.Products.Commands;

/// <summary>
/// Command that creates a new product.
/// </summary>
/// <param name="Sku">Stock keeping unit; must be unique.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Identifier of the product category.</param>
/// <param name="BaseUnit">Unit for stock and sale price.</param>
/// <param name="PurchaseUnit">Unit used when buying from suppliers.</param>
/// <param name="PurchaseUnitFactor">Base units contained in one purchase unit.</param>
/// <param name="PurchasePrice">Supplier price per purchase unit.</param>
/// <param name="SalePrice">Customer price per base unit.</param>
/// <param name="TaxRate">Tax percentage (0-100).</param>
/// <param name="InitialStock">Opening stock balance, in base units.</param>
/// <param name="MinimumStock">Low-stock threshold, in base units.</param>
public record CreateProductCommand(
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
    decimal InitialStock,
    decimal MinimumStock);
