using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;

namespace StockFlow.Application.Customers.Queries;

/// <summary>
/// Handles <see cref="GetCustomerByIdQuery"/>: projects a customer into a DTO, or null when missing.
/// </summary>
public class GetCustomerByIdHandler
{
    // Persistence abstraction used to build the query.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public GetCustomerByIdHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves a customer by its identifier.
    /// </summary>
    /// <param name="query">Identifier of the customer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The customer DTO, or null when it does not exist.</returns>
    public async Task<CustomerDto?> HandleAsync(
        GetCustomerByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _db.Customers
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new CustomerDto(
                c.Id,
                c.Name,
                c.TaxId,
                c.Phone,
                c.Email,
                c.Address,
                c.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
