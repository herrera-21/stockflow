using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Products.Commands;

/// <summary>
/// Handles <see cref="CreateProductCommand"/>: rejects duplicate SKUs and persists a new product.
/// </summary>
public class CreateProductHandler
{
    // Persistence abstraction used to check SKU uniqueness and save the new product.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public CreateProductHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Creates a product unless its SKU is already taken.
    /// </summary>
    /// <param name="command">Data for the new product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the created product, or a failure with a stable error code.</returns>
    public async Task<OperationResult<ProductDto>> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var sku = command.Sku.Trim();

        var skuAlreadyExists = await _db.Products
            .AnyAsync(p => p.Sku == sku, cancellationToken);

        if (skuAlreadyExists)
        {
            return OperationResult<ProductDto>.Failure(ProductErrorCodes.SkuAlreadyExists);
        }

        var product = Product.Create(
            sku,
            command.Name,
            command.Description,
            command.Category,
            command.PurchasePrice,
            command.SalePrice,
            command.TaxRate,
            command.InitialStock,
            command.MinimumStock);

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<ProductDto>.Success(product.ToDto());
    }
}
