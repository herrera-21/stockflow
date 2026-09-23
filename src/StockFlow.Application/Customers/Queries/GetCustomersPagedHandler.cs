using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Customers.Queries;

/// <summary>
/// Handles <see cref="GetCustomersPagedQuery"/>: filters, orders and pages customers in the database.
/// </summary>
public class GetCustomersPagedHandler
{
    // Persistence abstraction used to build the query.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public GetCustomersPagedHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the requested page of customers, applying the optional search filter.
    /// </summary>
    /// <param name="query">Page number, page size and optional search text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page of matching customers.</returns>
    public async Task<PagedResult<CustomerDto>> HandleAsync(
        GetCustomersPagedQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        // Filtering and paging stay in the database; only the requested page is materialized.
        IQueryable<Customer> customers = _db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Lowercasing keeps the match case-insensitive across providers (SQL Server and InMemory).
            var search = query.Search.Trim().ToLowerInvariant();
            customers = customers.Where(c =>
                c.Name.ToLower().Contains(search)
                || c.TaxId.ToLower().Contains(search)
                || (c.Email != null && c.Email.ToLower().Contains(search)));
        }

        var ordered = customers.OrderBy(c => c.Name);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerDto(
                c.Id,
                c.Name,
                c.TaxId,
                c.Phone,
                c.Email,
                c.Address,
                c.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>(items, page, pageSize, totalCount);
    }
}
