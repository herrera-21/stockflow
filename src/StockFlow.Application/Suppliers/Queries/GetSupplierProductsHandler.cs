using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;

namespace StockFlow.Application.Suppliers.Queries;

/// <summary>
/// Handles <see cref="GetSupplierProductsQuery"/>: projects the products associated with a supplier,
/// ordered by product name.
/// </summary>
public class GetSupplierProductsHandler
{
    // Persistence abstraction used to build the query.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public GetSupplierProductsHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the products associated with the given supplier.
    /// </summary>
    /// <param name="query">Identifier of the supplier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of associations, ordered by product name.</returns>
    public async Task<IReadOnlyList<SupplierProductDto>> HandleAsync(
        GetSupplierProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _db.ProductSuppliers
            .AsNoTracking()
            .Where(ps => ps.SupplierId == query.SupplierId)
            .Join(
                _db.Products,
                ps => ps.ProductId,
                p => p.Id,
                (ps, p) => new { Association = ps, Product = p })
            .OrderBy(x => x.Product.Name)
            .Select(x => new SupplierProductDto(
                x.Product.Id,
                x.Product.Name,
                x.Product.Sku,
                x.Association.SupplierSku,
                x.Association.PurchasePrice,
                x.Association.IsPreferred))
            .ToListAsync(cancellationToken);
    }
}
