using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Suppliers.Commands;

/// <summary>
/// Handles <see cref="CreateSupplierCommand"/>: rejects duplicate tax identifications and persists a
/// new supplier.
/// </summary>
public class CreateSupplierHandler
{
    // Persistence abstraction used to check tax-id uniqueness and save the new supplier.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public CreateSupplierHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Creates a supplier unless its tax identification is already taken.
    /// </summary>
    /// <param name="command">Data for the new supplier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the created supplier, or a failure with a stable error code.</returns>
    public async Task<OperationResult<SupplierDto>> HandleAsync(
        CreateSupplierCommand command,
        CancellationToken cancellationToken = default)
    {
        var taxId = command.TaxId.Trim();

        var taxIdAlreadyExists = await _db.Suppliers
            .AnyAsync(s => s.TaxId == taxId, cancellationToken);

        if (taxIdAlreadyExists)
        {
            return OperationResult<SupplierDto>.Failure(SupplierErrorCodes.TaxIdAlreadyExists);
        }

        var supplier = Supplier.Create(
            command.Name,
            taxId,
            command.ContactName,
            command.Phone,
            command.Email,
            command.Address);

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<SupplierDto>.Success(supplier.ToDto());
    }
}
