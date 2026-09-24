using StockFlow.Domain.Entities;

namespace StockFlow.Application.Suppliers;

/// <summary>
/// Maps supplier domain entities to their DTO representation.
/// </summary>
internal static class SupplierMapping
{
    /// <summary>Projects a <see cref="Supplier"/> into a <see cref="SupplierDto"/>.</summary>
    /// <param name="supplier">Supplier to project.</param>
    /// <returns>The projected DTO.</returns>
    public static SupplierDto ToDto(this Supplier supplier) => new(
        supplier.Id,
        supplier.Name,
        supplier.DocumentType,
        supplier.TaxId,
        supplier.ContactName,
        supplier.Phone,
        supplier.Email,
        supplier.Address,
        supplier.IsActive);
}
