using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Categories.Commands;

/// <summary>
/// Handles <see cref="CreateCategoryCommand"/>: rejects duplicate names and persists a new category.
/// </summary>
public class CreateCategoryHandler
{
    // Persistence abstraction used to check name uniqueness and save the new category.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public CreateCategoryHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Creates a category unless its name is already taken (ignoring case).
    /// </summary>
    /// <param name="command">Data for the new category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the created category, or a failure with a stable error code.</returns>
    public async Task<OperationResult<CategoryDto>> HandleAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var name = command.Name.Trim();

        // Compared in lower case so the check is case-insensitive on every provider, matching the
        // case-insensitive collation of SQL Server.
        var nameAlreadyExists = await _db.Categories
            .AnyAsync(c => c.Name.ToLower() == name.ToLower(), cancellationToken);

        if (nameAlreadyExists)
        {
            return OperationResult<CategoryDto>.Failure(CategoryErrorCodes.NameAlreadyExists);
        }

        var category = Category.Create(name);

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<CategoryDto>.Success(category.ToDto());
    }
}
