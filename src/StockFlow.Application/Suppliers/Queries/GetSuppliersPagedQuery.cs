namespace StockFlow.Application.Suppliers.Queries;

/// <summary>
/// Query for a page of suppliers. Search matches name, tax identification, contact name or email and
/// is optional.
/// </summary>
/// <param name="Page">One-based page number.</param>
/// <param name="PageSize">Maximum number of items per page.</param>
/// <param name="Search">Optional text matched against name, tax identification, contact name or email.</param>
public record GetSuppliersPagedQuery(int Page, int PageSize, string? Search = null);
