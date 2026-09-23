using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Customers.Commands;

/// <summary>
/// Handles <see cref="CreateCustomerCommand"/>: rejects duplicate tax identifications and persists a
/// new customer.
/// </summary>
public class CreateCustomerHandler
{
    // Persistence abstraction used to check tax-id uniqueness and save the new customer.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public CreateCustomerHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Creates a customer unless its tax identification is already taken.
    /// </summary>
    /// <param name="command">Data for the new customer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the created customer, or a failure with a stable error code.</returns>
    public async Task<OperationResult<CustomerDto>> HandleAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        var taxId = command.TaxId.Trim();

        var taxIdAlreadyExists = await _db.Customers
            .AnyAsync(c => c.TaxId == taxId, cancellationToken);

        if (taxIdAlreadyExists)
        {
            return OperationResult<CustomerDto>.Failure(CustomerErrorCodes.TaxIdAlreadyExists);
        }

        var customer = Customer.Create(
            command.Name,
            taxId,
            command.Phone,
            command.Email,
            command.Address);

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<CustomerDto>.Success(customer.ToDto());
    }
}
