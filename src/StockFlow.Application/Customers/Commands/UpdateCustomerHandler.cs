using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;
using StockFlow.Domain;

namespace StockFlow.Application.Customers.Commands;

/// <summary>
/// Handles <see cref="UpdateCustomerCommand"/>: loads the customer, checks tax-id uniqueness among
/// other customers and applies the editable data.
/// </summary>
public class UpdateCustomerHandler
{
    // Persistence abstraction used to load and save the customer.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public UpdateCustomerHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Updates a customer unless it does not exist or the tax identification belongs to another
    /// customer.
    /// </summary>
    /// <param name="command">Data to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the updated customer, or a failure with a stable error code.</returns>
    public async Task<OperationResult<CustomerDto>> HandleAsync(
        UpdateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (customer is null)
        {
            return OperationResult<CustomerDto>.Failure(CustomerErrorCodes.NotFound);
        }

        var taxId = DocumentValidation.Normalize(command.DocumentType, command.TaxId);

        var taxIdAlreadyExists = await _db.Customers
            .AnyAsync(c => c.DocumentType == command.DocumentType && c.TaxId == taxId && c.Id != command.Id, cancellationToken);

        if (taxIdAlreadyExists)
        {
            return OperationResult<CustomerDto>.Failure(CustomerErrorCodes.TaxIdAlreadyExists);
        }

        customer.Update(
            command.Name,
            command.DocumentType,
            taxId,
            command.Phone,
            command.Email,
            command.Address);

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<CustomerDto>.Success(customer.ToDto());
    }
}
