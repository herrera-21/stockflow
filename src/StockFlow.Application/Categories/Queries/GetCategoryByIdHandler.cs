using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;

namespace StockFlow.Application.Categories.Queries;

/// <summary>
/// Handles <see cref="GetCategoryByIdQuery"/>: projects a category into a DTO, or null when missing.
/// </summary>
public class GetCategoryByIdHandler
{
    // Persistence abstraction used to build the query.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public GetCategoryByIdHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves a category by its identifier.
    /// </summary>
    /// <param name="query">Identifier of the category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category DTO, or null when it does not exist.</returns>
    public async Task<CategoryDto?> HandleAsync(
        GetCategoryByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new CategoryDto(
                c.Id,
                c.Code,
                c.Name,
                c.IsActive,
                _db.Products.Count(p => p.CategoryId == c.Id)))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
