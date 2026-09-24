using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;
using StockFlow.Domain;

namespace StockFlow.Application.Suppliers.Commands;

/// <summary>
/// Handles <see cref="UpdateSupplierCommand"/>: loads the supplier, checks tax-id uniqueness among
/// other suppliers and applies the editable data.
/// </summary>
public class UpdateSupplierHandler
{
    // Persistence abstraction used to load and save the supplier.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public UpdateSupplierHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Updates a supplier unless it does not exist or the tax identification belongs to another
    /// supplier.
    /// </summary>
    /// <param name="command">Data to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the updated supplier, or a failure with a stable error code.</returns>
    public async Task<OperationResult<SupplierDto>> HandleAsync(
        UpdateSupplierCommand command,
        CancellationToken cancellationToken = default)
    {
        var supplier = await _db.Suppliers
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (supplier is null)
        {
            return OperationResult<SupplierDto>.Failure(SupplierErrorCodes.NotFound);
        }

        var taxId = DocumentValidation.Normalize(command.DocumentType, command.TaxId);

        var taxIdAlreadyExists = await _db.Suppliers
            .AnyAsync(s => s.DocumentType == command.DocumentType && s.TaxId == taxId && s.Id != command.Id, cancellationToken);

        if (taxIdAlreadyExists)
        {
            return OperationResult<SupplierDto>.Failure(SupplierErrorCodes.TaxIdAlreadyExists);
        }

        supplier.Update(
            command.Name,
            command.DocumentType,
            taxId,
            command.ContactName,
            command.Phone,
            command.Email,
            command.Address);

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<SupplierDto>.Success(supplier.ToDto());
    }
}
