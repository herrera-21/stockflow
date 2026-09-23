namespace StockFlow.Application.Customers.Commands;

/// <summary>
/// Command that updates an existing customer.
/// </summary>
/// <param name="Id">Identifier of the customer to update.</param>
/// <param name="Name">Display name.</param>
/// <param name="TaxId">Tax identification; must be unique.</param>
/// <param name="Phone">Optional phone number.</param>
/// <param name="Email">Optional email address.</param>
/// <param name="Address">Optional postal address.</param>
public record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string TaxId,
    string? Phone,
    string? Email,
    string? Address);
