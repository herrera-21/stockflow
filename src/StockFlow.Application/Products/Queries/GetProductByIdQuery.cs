namespace StockFlow.Application.Products.Queries;

/// <summary>
/// Query that retrieves a single product by its identifier.
/// </summary>
/// <param name="Id">Identifier of the product.</param>
public record GetProductByIdQuery(Guid Id);
