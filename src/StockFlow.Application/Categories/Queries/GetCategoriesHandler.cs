using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;

namespace StockFlow.Application.Categories.Queries;

/// <summary>
/// Handles <see cref="GetCategoriesQuery"/>: returns the active categories ordered by name.
/// </summary>
public class GetCategoriesHandler
{
    // Persistence abstraction used to build the query.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public GetCategoriesHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the active categories, ordered by name, for use in a selector.
    /// </summary>
    /// <param name="query">Query marker.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active categories.</returns>
    public async Task<IReadOnlyList<CategoryDto>> HandleAsync(
        GetCategoriesQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Code, c.Name, c.IsActive, 0))
            .ToListAsync(cancellationToken);
    }
}
