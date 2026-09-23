namespace StockFlow.Application.Customers.Commands;

/// <summary>
/// Command that soft-deletes (deactivates) a customer.
/// </summary>
/// <param name="Id">Identifier of the customer to deactivate.</param>
public record DeactivateCustomerCommand(Guid Id);
