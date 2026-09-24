using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Categories.Queries;

/// <summary>
/// Handles <see cref="GetCategoriesPagedQuery"/>: filters, orders and pages categories in the
/// database, including how many products each one has.
/// </summary>
public class GetCategoriesPagedHandler
{
    // Persistence abstraction used to build the query.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public GetCategoriesPagedHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the requested page of categories, applying the optional name filter.
    /// </summary>
    /// <param name="query">Page number, page size and optional search text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page of matching categories.</returns>
    public async Task<PagedResult<CategoryDto>> HandleAsync(
        GetCategoriesPagedQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        IQueryable<Category> categories = _db.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Lowercasing keeps the match case-insensitive across providers (SQL Server and InMemory).
            var search = query.Search.Trim().ToLowerInvariant();
            categories = categories.Where(c => c.Name.ToLower().Contains(search));
        }

        var ordered = categories.OrderBy(c => c.Name);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoryDto(
                c.Id,
                c.Code,
                c.Name,
                c.IsActive,
                _db.Products.Count(p => p.CategoryId == c.Id)))
            .ToListAsync(cancellationToken);

        return new PagedResult<CategoryDto>(items, page, pageSize, totalCount);
    }
}
