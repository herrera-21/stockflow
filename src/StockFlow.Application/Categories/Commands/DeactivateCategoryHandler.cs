using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;

namespace StockFlow.Application.Categories.Commands;

/// <summary>
/// Handles <see cref="DeactivateCategoryCommand"/>: soft-deletes a category by marking it inactive,
/// refusing to do so while products are still associated with it.
/// </summary>
public class DeactivateCategoryHandler
{
    // Persistence abstraction used to load the category and count its products.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public DeactivateCategoryHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Deactivates a category unless it does not exist or still has associated products.
    /// </summary>
    /// <param name="command">Identifier of the category to deactivate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the deactivated category, or a failure with a stable error code.</returns>
    public async Task<OperationResult<CategoryDto>> HandleAsync(
        DeactivateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category is null)
        {
            return OperationResult<CategoryDto>.Failure(CategoryErrorCodes.NotFound);
        }

        // Referential safety: a category in use cannot be deactivated, even if the products that
        // reference it are themselves inactive.
        var productCount = await _db.Products
            .CountAsync(p => p.CategoryId == command.Id, cancellationToken);

        if (productCount > 0)
        {
            return OperationResult<CategoryDto>.Failure(CategoryErrorCodes.InUse);
        }

        category.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<CategoryDto>.Success(category.ToDto());
    }
}
