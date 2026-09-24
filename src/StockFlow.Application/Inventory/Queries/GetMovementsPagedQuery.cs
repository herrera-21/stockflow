namespace StockFlow.Application.Inventory.Queries;

/// <summary>
/// Query for a page of a product's inventory movements, newest first.
/// </summary>
/// <param name="ProductId">Identifier of the product.</param>
/// <param name="Page">One-based page number.</param>
/// <param name="PageSize">Maximum number of items per page.</param>
public record GetMovementsPagedQuery(Guid ProductId, int Page, int PageSize);
