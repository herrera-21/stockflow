using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;
using StockFlow.Application.Products;

namespace StockFlow.Application.Suppliers.Commands;

/// <summary>
/// Handles <see cref="RemoveSupplierProductCommand"/>: removes the association between a supplier and
/// a product.
/// </summary>
public class RemoveSupplierProductHandler
{
    // Persistence abstraction used to load the product and save the change.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public RemoveSupplierProductHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Removes the association unless the product does not exist.
    /// </summary>
    /// <param name="command">Supplier and product identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the product identifier, or a failure with a stable error code.</returns>
    public async Task<OperationResult<Guid>> HandleAsync(
        RemoveSupplierProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .Include(p => p.Suppliers)
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);

        if (product is null)
        {
            return OperationResult<Guid>.Failure(ProductErrorCodes.NotFound);
        }

        product.RemoveSupplier(command.SupplierId);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<Guid>.Success(product.Id);
    }
}
