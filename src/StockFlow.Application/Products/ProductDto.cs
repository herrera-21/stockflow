using StockFlow.Domain;

namespace StockFlow.Application.Products;

/// <summary>
/// Product data transferred across the Application boundary.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="Sku">Stock keeping unit.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Identifier of the product category.</param>
/// <param name="CategoryCode">Stable code of the category; null for user-created ones.</param>
/// <param name="CategoryName">Display name of the category.</param>
/// <param name="BaseUnit">Unit for stock and sale price.</param>
/// <param name="PurchaseUnit">Unit used when buying from suppliers.</param>
/// <param name="PurchaseUnitFactor">Base units contained in one purchase unit.</param>
/// <param name="PurchasePrice">Price paid to the supplier per purchase unit.</param>
/// <param name="UnitCost">Cost of one base unit (purchase price divided by the factor).</param>
/// <param name="SalePrice">Price charged to the customer per base unit.</param>
/// <param name="TaxRate">Tax percentage (0-100).</param>
/// <param name="CurrentStock">Quantity currently in stock, in base units.</param>
/// <param name="MinimumStock">Low-stock threshold, in base units.</param>
/// <param name="IsActive">Whether the product is active.</param>
/// <param name="HasLowStock">True when stock is at or below the minimum.</param>
public record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    Guid CategoryId,
    string? CategoryCode,
    string CategoryName,
    UnitOfMeasure BaseUnit,
    UnitOfMeasure PurchaseUnit,
    decimal PurchaseUnitFactor,
    decimal PurchasePrice,
    decimal UnitCost,
    decimal SalePrice,
    decimal TaxRate,
    decimal CurrentStock,
    decimal MinimumStock,
    bool IsActive,
    bool HasLowStock);
