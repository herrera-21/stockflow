namespace StockFlow.Application.Common.Models;

/// <summary>
/// A single page of results together with the paging metadata needed to render navigation.
/// </summary>
/// <typeparam name="T">Type of the items contained in the page.</typeparam>
/// <param name="Items">Items in the current page.</param>
/// <param name="Page">One-based page number.</param>
/// <param name="PageSize">Maximum number of items per page.</param>
/// <param name="TotalCount">Total number of items across all pages.</param>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>Total number of pages for the current page size.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>True when there is a page before the current one.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>True when there is a page after the current one.</summary>
    public bool HasNextPage => Page < TotalPages;
}
