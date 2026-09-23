namespace StockFlow.Application.Suppliers.Commands;

/// <summary>
/// Command that updates an existing supplier.
/// </summary>
/// <param name="Id">Identifier of the supplier to update.</param>
/// <param name="Name">Company name.</param>
/// <param name="TaxId">Tax identification; must be unique.</param>
/// <param name="ContactName">Optional contact person name.</param>
/// <param name="Phone">Optional phone number.</param>
/// <param name="Email">Optional email address.</param>
/// <param name="Address">Optional postal address.</param>
public record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string TaxId,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address);
