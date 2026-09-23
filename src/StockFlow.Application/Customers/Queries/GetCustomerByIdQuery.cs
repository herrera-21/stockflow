namespace StockFlow.Application.Customers.Queries;

/// <summary>
/// Query that retrieves a single customer by its identifier.
/// </summary>
/// <param name="Id">Identifier of the customer.</param>
public record GetCustomerByIdQuery(Guid Id);
