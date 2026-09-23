namespace StockFlow.Application.Products.Queries;

/// <summary>
/// Query for a page of products. Search matches name or SKU; category filters by category. Both
/// filters are optional.
/// </summary>
/// <param name="Page">One-based page number.</param>
/// <param name="PageSize">Maximum number of items per page.</param>
/// <param name="Search">Optional text matched against name or SKU.</param>
/// <param name="Category">Optional category filter.</param>
public record GetProductsPagedQuery(int Page, int PageSize, string? Search = null, string? Category = null);
