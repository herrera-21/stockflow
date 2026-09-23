using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;

namespace StockFlow.Application.Customers.Commands;

/// <summary>
/// Handles <see cref="DeactivateCustomerCommand"/>: soft-deletes a customer by marking it inactive.
/// </summary>
public class DeactivateCustomerHandler
{
    // Persistence abstraction used to load and save the customer.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public DeactivateCustomerHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Deactivates a customer unless it does not exist.
    /// </summary>
    /// <param name="command">Identifier of the customer to deactivate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the deactivated customer, or a failure with a stable error code.</returns>
    public async Task<OperationResult<CustomerDto>> HandleAsync(
        DeactivateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (customer is null)
        {
            return OperationResult<CustomerDto>.Failure(CustomerErrorCodes.NotFound);
        }

        customer.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<CustomerDto>.Success(customer.ToDto());
    }
}
