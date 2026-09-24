namespace StockFlow.Application.Categories.Queries;

/// <summary>
/// Query that returns a page of categories, optionally filtered by name.
/// </summary>
/// <param name="Page">One-based page number.</param>
/// <param name="PageSize">Maximum number of items per page.</param>
/// <param name="Search">Optional name filter.</param>
public record GetCategoriesPagedQuery(int Page, int PageSize, string? Search = null);
