using StockFlow.Domain.Entities;

namespace StockFlow.Application.Customers;

/// <summary>
/// Maps customer domain entities to their DTO representation.
/// </summary>
internal static class CustomerMapping
{
    /// <summary>Projects a <see cref="Customer"/> into a <see cref="CustomerDto"/>.</summary>
    /// <param name="customer">Customer to project.</param>
    /// <returns>The projected DTO.</returns>
    public static CustomerDto ToDto(this Customer customer) => new(
        customer.Id,
        customer.Name,
        customer.DocumentType,
        customer.TaxId,
        customer.Phone,
        customer.Email,
        customer.Address,
        customer.IsActive);
}
