using StockFlow.Domain;

namespace StockFlow.Application.Suppliers;

/// <summary>
/// Association between a supplier and one of the products it offers, transferred across the
/// Application boundary.
/// </summary>
/// <param name="ProductId">Identifier of the associated product.</param>
/// <param name="ProductName">Name of the associated product.</param>
/// <param name="ProductSku">SKU of the associated product.</param>
/// <param name="SupplierSku">Optional reference the supplier uses for the product.</param>
/// <param name="PurchasePrice">Optional purchase price agreed with the supplier, per purchase unit.</param>
/// <param name="PurchaseUnit">Unit in which the product is bought, which the price refers to.</param>
/// <param name="IsPreferred">Whether this supplier is the preferred one for the product.</param>
public record SupplierProductDto(
    Guid ProductId,
    string ProductName,
    string ProductSku,
    string? SupplierSku,
    decimal? PurchasePrice,
    UnitOfMeasure PurchaseUnit,
    bool IsPreferred);
