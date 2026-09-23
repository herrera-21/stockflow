namespace StockFlow.Application.Suppliers.Queries;

/// <summary>
/// Query that retrieves a single supplier by its identifier.
/// </summary>
/// <param name="Id">Identifier of the supplier.</param>
public record GetSupplierByIdQuery(Guid Id);
