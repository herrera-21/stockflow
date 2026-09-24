using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;

namespace StockFlow.Application.Products.Commands;

/// <summary>
/// Handles <see cref="DeactivateProductCommand"/>: soft-deletes a product by marking it inactive.
/// </summary>
public class DeactivateProductHandler
{
    // Persistence abstraction used to load and save the product.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public DeactivateProductHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Deactivates a product unless it does not exist.
    /// </summary>
    /// <param name="command">Identifier of the product to deactivate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the deactivated product, or a failure with a stable error code.</returns>
    public async Task<OperationResult<ProductDto>> HandleAsync(
        DeactivateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        // The category is loaded so the returned DTO can render the row without another query.
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (product is null)
        {
            return OperationResult<ProductDto>.Failure(ProductErrorCodes.NotFound);
        }

        product.Deactivate();

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another operation changed the product between the read and the save.
            return OperationResult<ProductDto>.Failure(ProductErrorCodes.ConcurrencyConflict);
        }

        return OperationResult<ProductDto>.Success(product.ToDto());
    }
}
