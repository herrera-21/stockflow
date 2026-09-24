using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;

namespace StockFlow.Application.Categories.Commands;

/// <summary>
/// Handles <see cref="UpdateCategoryCommand"/>: loads the category, checks name uniqueness among
/// other categories and renames it.
/// </summary>
public class UpdateCategoryHandler
{
    // Persistence abstraction used to load and save the category.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public UpdateCategoryHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Renames a category unless it does not exist or the name belongs to another category.
    /// </summary>
    /// <param name="command">Data to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the updated category, or a failure with a stable error code.</returns>
    public async Task<OperationResult<CategoryDto>> HandleAsync(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category is null)
        {
            return OperationResult<CategoryDto>.Failure(CategoryErrorCodes.NotFound);
        }

        var name = command.Name.Trim();

        var nameAlreadyExists = await _db.Categories
            .AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.Id != command.Id, cancellationToken);

        if (nameAlreadyExists)
        {
            return OperationResult<CategoryDto>.Failure(CategoryErrorCodes.NameAlreadyExists);
        }

        category.Rename(name);

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<CategoryDto>.Success(category.ToDto());
    }
}
