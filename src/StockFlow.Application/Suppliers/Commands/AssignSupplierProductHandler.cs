using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;
using StockFlow.Application.Products;

namespace StockFlow.Application.Suppliers.Commands;

/// <summary>
/// Handles <see cref="AssignSupplierProductCommand"/>: associates a product with a supplier (or
/// updates the existing association), enforcing the single-preferred-supplier rule in the domain.
/// </summary>
public class AssignSupplierProductHandler
{
    // Persistence abstraction used to load the supplier and product, and to save the association.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public AssignSupplierProductHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Associates a product with a supplier unless either of them does not exist.
    /// </summary>
    /// <param name="command">Supplier, product and supplier-specific data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the association, or a failure with a stable error code.</returns>
    public async Task<OperationResult<SupplierProductDto>> HandleAsync(
        AssignSupplierProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var supplierExists = await _db.Suppliers
            .AnyAsync(s => s.Id == command.SupplierId, cancellationToken);

        if (!supplierExists)
        {
            return OperationResult<SupplierProductDto>.Failure(SupplierErrorCodes.NotFound);
        }

        var product = await _db.Products
            .Include(p => p.Suppliers)
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);

        if (product is null)
        {
            return OperationResult<SupplierProductDto>.Failure(ProductErrorCodes.NotFound);
        }

        product.AssignSupplier(
            command.SupplierId,
            command.SupplierSku,
            command.PurchasePrice,
            command.IsPreferred);

        await _db.SaveChangesAsync(cancellationToken);

        var association = product.Suppliers.Single(s => s.SupplierId == command.SupplierId);
        var dto = new SupplierProductDto(
            product.Id,
            product.Name,
            product.Sku,
            association.SupplierSku,
            association.PurchasePrice,
            product.PurchaseUnit,
            association.IsPreferred);

        return OperationResult<SupplierProductDto>.Success(dto);
    }
}
