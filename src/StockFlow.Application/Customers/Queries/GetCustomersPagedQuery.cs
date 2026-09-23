namespace StockFlow.Application.Customers.Queries;

/// <summary>
/// Query for a page of customers. Search matches name, tax identification or email and is optional.
/// </summary>
/// <param name="Page">One-based page number.</param>
/// <param name="PageSize">Maximum number of items per page.</param>
/// <param name="Search">Optional text matched against name, tax identification or email.</param>
public record GetCustomersPagedQuery(int Page, int PageSize, string? Search = null);
