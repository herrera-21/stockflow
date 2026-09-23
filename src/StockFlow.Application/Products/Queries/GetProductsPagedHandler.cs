using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Products.Queries;

/// <summary>
/// Handles <see cref="GetProductsPagedQuery"/>: filters, orders and pages products in the database.
/// </summary>
public class GetProductsPagedHandler
{
    // Persistence abstraction used to build the query.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public GetProductsPagedHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the requested page of products, applying the optional filters.
    /// </summary>
    /// <param name="query">Page number, page size and optional filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page of matching products.</returns>
    public async Task<PagedResult<ProductDto>> HandleAsync(
        GetProductsPagedQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        // Filtering and paging stay in the database; only the requested page is materialized.
        IQueryable<Product> products = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Lowercasing keeps the match case-insensitive across providers (SQL Server and InMemory).
            var search = query.Search.Trim().ToLowerInvariant();
            products = products.Where(p =>
                p.Name.ToLower().Contains(search) || p.Sku.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim().ToLowerInvariant();
            products = products.Where(p => p.Category.ToLower().Contains(category));
        }

        var ordered = products.OrderBy(p => p.Name);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto(
                p.Id,
                p.Sku,
                p.Name,
                p.Description,
                p.Category,
                p.PurchasePrice,
                p.SalePrice,
                p.TaxRate,
                p.CurrentStock,
                p.MinimumStock,
                p.IsActive,
                p.CurrentStock <= p.MinimumStock))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, page, pageSize, totalCount);
    }
}
