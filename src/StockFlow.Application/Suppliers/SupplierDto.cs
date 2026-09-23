namespace StockFlow.Application.Suppliers;

/// <summary>
/// Supplier data transferred across the Application boundary.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="Name">Company name.</param>
/// <param name="TaxId">Tax identification.</param>
/// <param name="ContactName">Optional contact person name.</param>
/// <param name="Phone">Optional phone number.</param>
/// <param name="Email">Optional email address.</param>
/// <param name="Address">Optional postal address.</param>
/// <param name="IsActive">Whether the supplier is active.</param>
public record SupplierDto(
    Guid Id,
    string Name,
    string TaxId,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    bool IsActive);
