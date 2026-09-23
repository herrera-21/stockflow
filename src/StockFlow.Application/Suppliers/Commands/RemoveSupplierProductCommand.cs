namespace StockFlow.Application.Suppliers.Commands;

/// <summary>
/// Command that removes the association between a supplier and a product.
/// </summary>
/// <param name="SupplierId">Identifier of the supplier.</param>
/// <param name="ProductId">Identifier of the product.</param>
public record RemoveSupplierProductCommand(Guid SupplierId, Guid ProductId);
