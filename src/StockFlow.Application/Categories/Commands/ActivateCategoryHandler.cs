using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;

namespace StockFlow.Application.Categories.Commands;

/// <summary>
/// Handles <see cref="ActivateCategoryCommand"/>: reactivates a previously deactivated category.
/// </summary>
public class ActivateCategoryHandler
{
    // Persistence abstraction used to load and save the category.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public ActivateCategoryHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Activates a category unless it does not exist.
    /// </summary>
    /// <param name="command">Identifier of the category to activate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the activated category, or a failure with a stable error code.</returns>
    public async Task<OperationResult<CategoryDto>> HandleAsync(
        ActivateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category is null)
        {
            return OperationResult<CategoryDto>.Failure(CategoryErrorCodes.NotFound);
        }

        category.Activate();
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<CategoryDto>.Success(category.ToDto());
    }
}
