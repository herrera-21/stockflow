using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;

namespace StockFlow.Application.Products.Queries;

/// <summary>
/// Handles <see cref="GetProductByIdQuery"/>: projects a product into a DTO, or null when missing.
/// </summary>
public class GetProductByIdHandler
{
    // Persistence abstraction used to build the query.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public GetProductByIdHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves a product by its identifier.
    /// </summary>
    /// <param name="query">Identifier of the product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The product DTO, or null when it does not exist.</returns>
    public async Task<ProductDto?> HandleAsync(
        GetProductByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == query.Id)
            .Select(p => new ProductDto(
                p.Id,
                p.Sku,
                p.Name,
                p.Description,
                p.Category,
                p.PurchasePrice,
                p.SalePrice,
                p.TaxRate,
                p.CurrentStock,
                p.MinimumStock,
                p.IsActive,
                p.CurrentStock <= p.MinimumStock))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
