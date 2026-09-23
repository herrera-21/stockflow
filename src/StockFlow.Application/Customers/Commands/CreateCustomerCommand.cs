namespace StockFlow.Application.Customers.Commands;

/// <summary>
/// Command that creates a new customer.
/// </summary>
/// <param name="Name">Display name.</param>
/// <param name="TaxId">Tax identification; must be unique.</param>
/// <param name="Phone">Optional phone number.</param>
/// <param name="Email">Optional email address.</param>
/// <param name="Address">Optional postal address.</param>
public record CreateCustomerCommand(
    string Name,
    string TaxId,
    string? Phone,
    string? Email,
    string? Address);
