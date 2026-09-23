namespace StockFlow.Application.Suppliers.Commands;

/// <summary>
/// Command that associates a product with a supplier, or updates the association when it already
/// exists.
/// </summary>
/// <param name="SupplierId">Identifier of the supplier.</param>
/// <param name="ProductId">Identifier of the product.</param>
/// <param name="SupplierSku">Optional reference the supplier uses for the product.</param>
/// <param name="PurchasePrice">Optional purchase price agreed with the supplier.</param>
/// <param name="IsPreferred">Whether this supplier becomes the preferred one for the product.</param>
public record AssignSupplierProductCommand(
    Guid SupplierId,
    Guid ProductId,
    string? SupplierSku,
    decimal? PurchasePrice,
    bool IsPreferred);
