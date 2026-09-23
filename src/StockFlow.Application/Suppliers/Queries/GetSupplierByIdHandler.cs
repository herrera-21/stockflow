using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;

namespace StockFlow.Application.Suppliers.Queries;

/// <summary>
/// Handles <see cref="GetSupplierByIdQuery"/>: projects a supplier into a DTO, or null when missing.
/// </summary>
public class GetSupplierByIdHandler
{
    // Persistence abstraction used to build the query.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public GetSupplierByIdHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves a supplier by its identifier.
    /// </summary>
    /// <param name="query">Identifier of the supplier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The supplier DTO, or null when it does not exist.</returns>
    public async Task<SupplierDto?> HandleAsync(
        GetSupplierByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _db.Suppliers
            .AsNoTracking()
            .Where(s => s.Id == query.Id)
            .Select(s => new SupplierDto(
                s.Id,
                s.Name,
                s.TaxId,
                s.ContactName,
                s.Phone,
                s.Email,
                s.Address,
                s.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
