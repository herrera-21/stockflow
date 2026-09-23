using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;

namespace StockFlow.Application.Suppliers.Commands;

/// <summary>
/// Handles <see cref="DeactivateSupplierCommand"/>: soft-deletes a supplier by marking it inactive.
/// </summary>
public class DeactivateSupplierHandler
{
    // Persistence abstraction used to load and save the supplier.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public DeactivateSupplierHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Deactivates a supplier unless it does not exist.
    /// </summary>
    /// <param name="command">Identifier of the supplier to deactivate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the deactivated supplier, or a failure with a stable error code.</returns>
    public async Task<OperationResult<SupplierDto>> HandleAsync(
        DeactivateSupplierCommand command,
        CancellationToken cancellationToken = default)
    {
        var supplier = await _db.Suppliers
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (supplier is null)
        {
            return OperationResult<SupplierDto>.Failure(SupplierErrorCodes.NotFound);
        }

        supplier.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<SupplierDto>.Success(supplier.ToDto());
    }
}
