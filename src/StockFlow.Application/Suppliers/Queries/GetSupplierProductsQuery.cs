namespace StockFlow.Application.Suppliers.Queries;

/// <summary>
/// Query that retrieves the products associated with a supplier.
/// </summary>
/// <param name="SupplierId">Identifier of the supplier.</param>
public record GetSupplierProductsQuery(Guid SupplierId);
