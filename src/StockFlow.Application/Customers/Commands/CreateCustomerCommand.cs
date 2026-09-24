using StockFlow.Domain;

namespace StockFlow.Application.Customers.Commands;

/// <summary>
/// Command that creates a new customer.
/// </summary>
/// <param name="Name">Display name.</param>
/// <param name="DocumentType">Type of the identity document.</param>
/// <param name="TaxId">Identity document number; unique per document type.</param>
/// <param name="Phone">Optional phone number.</param>
/// <param name="Email">Optional email address.</param>
/// <param name="Address">Optional postal address.</param>
public record CreateCustomerCommand(
    string Name,
    DocumentType DocumentType,
    string TaxId,
    string? Phone,
    string? Email,
    string? Address);
