using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;

namespace StockFlow.Application.Inventory.Queries;

/// <summary>
/// Handles <see cref="GetMovementsPagedQuery"/>: returns a page of a product's movements, newest
/// first, projected straight from the database.
/// </summary>
public class GetMovementsPagedHandler
{
    // Persistence abstraction used to build the query.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public GetMovementsPagedHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the requested page of movements for a product.
    /// </summary>
    /// <param name="query">Product and paging data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page of matching movements.</returns>
    public async Task<PagedResult<InventoryMovementDto>> HandleAsync(
        GetMovementsPagedQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        // Newest first; the identifier breaks ties between movements recorded in the same instant.
        var movements = _db.InventoryMovements
            .AsNoTracking()
            .Where(m => m.ProductId == query.ProductId)
            .OrderByDescending(m => m.OccurredAt)
            .ThenByDescending(m => m.Id);

        var totalCount = await movements.CountAsync(cancellationToken);

        var items = await movements
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new InventoryMovementDto(
                m.Id,
                m.ProductId,
                m.Type,
                m.Quantity,
                m.StockBefore,
                m.StockAfter,
                m.BaseUnit,
                m.Reason,
                m.Note,
                m.UserName,
                m.OccurredAt,
                m.ReferenceType,
                m.ReferenceId))
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryMovementDto>(items, page, pageSize, totalCount);
    }
}
