using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;

namespace StockFlow.Application.Products.Commands;

/// <summary>
/// Handles <see cref="UpdateProductCommand"/>: loads the product, checks SKU uniqueness among other
/// products, rejects a base unit change while there is stock and applies the editable data.
/// </summary>
public class UpdateProductHandler
{
    // Persistence abstraction used to load and save the product.
    private readonly IAppDbContext _db;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    public UpdateProductHandler(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Updates a product unless it does not exist or the SKU belongs to another product.
    /// </summary>
    /// <param name="command">Data to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the updated product, or a failure with a stable error code.</returns>
    public async Task<OperationResult<ProductDto>> HandleAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (product is null)
        {
            return OperationResult<ProductDto>.Failure(ProductErrorCodes.NotFound);
        }

        var sku = command.Sku.Trim();

        var skuAlreadyExists = await _db.Products
            .AnyAsync(p => p.Sku == sku && p.Id != command.Id, cancellationToken);

        if (skuAlreadyExists)
        {
            return OperationResult<ProductDto>.Failure(ProductErrorCodes.SkuAlreadyExists);
        }

        var category = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == command.CategoryId && c.IsActive, cancellationToken);

        if (category is null)
        {
            return OperationResult<ProductDto>.Failure(ProductErrorCodes.CategoryNotFound);
        }

        if (!product.CanChangeBaseUnitTo(command.BaseUnit))
        {
            return OperationResult<ProductDto>.Failure(ProductErrorCodes.BaseUnitLocked);
        }

        product.Update(
            sku,
            command.Name,
            command.Description,
            command.CategoryId,
            command.BaseUnit,
            command.PurchaseUnit,
            command.PurchaseUnitFactor,
            command.PurchasePrice,
            command.SalePrice,
            command.TaxRate,
            command.MinimumStock);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another operation changed the product between the read and the save.
            return OperationResult<ProductDto>.Failure(ProductErrorCodes.ConcurrencyConflict);
        }

        return OperationResult<ProductDto>.Success(product.ToDto(category));
    }
}
