using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Suppliers.Queries;

/// <summary>
/// Handles <see cref="GetSuppliersPagedQuery"/>: filters, orders and pages suppliers in the database.
/// </summary>
public class GetSuppliersPagedHandler
{
    // Persistence abstraction used to build the query.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public GetSuppliersPagedHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the requested page of suppliers, applying the optional search filter.
    /// </summary>
    /// <param name="query">Page number, page size and optional search text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page of matching suppliers.</returns>
    public async Task<PagedResult<SupplierDto>> HandleAsync(
        GetSuppliersPagedQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        // Filtering and paging stay in the database; only the requested page is materialized.
        IQueryable<Supplier> suppliers = _db.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Lowercasing keeps the match case-insensitive across providers (SQL Server and InMemory).
            var search = query.Search.Trim().ToLowerInvariant();
            suppliers = suppliers.Where(s =>
                s.Name.ToLower().Contains(search)
                || s.TaxId.ToLower().Contains(search)
                || (s.ContactName != null && s.ContactName.ToLower().Contains(search))
                || (s.Email != null && s.Email.ToLower().Contains(search)));
        }

        var ordered = suppliers.OrderBy(s => s.Name);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SupplierDto(
                s.Id,
                s.Name,
                s.TaxId,
                s.ContactName,
                s.Phone,
                s.Email,
                s.Address,
                s.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierDto>(items, page, pageSize, totalCount);
    }
}
