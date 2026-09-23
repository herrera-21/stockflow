namespace StockFlow.Application.Customers;

/// <summary>
/// Customer data transferred across the Application boundary.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="TaxId">Tax identification.</param>
/// <param name="Phone">Optional phone number.</param>
/// <param name="Email">Optional email address.</param>
/// <param name="Address">Optional postal address.</param>
/// <param name="IsActive">Whether the customer is active.</param>
public record CustomerDto(
    Guid Id,
    string Name,
    string TaxId,
    string? Phone,
    string? Email,
    string? Address,
    bool IsActive);
