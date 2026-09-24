using StockFlow.Domain.Entities;

namespace StockFlow.Application.Products;

/// <summary>
/// Maps product domain entities to their DTO representation.
/// </summary>
internal static class ProductMapping
{
    /// <summary>
    /// Projects a <see cref="Product"/> into a <see cref="ProductDto"/>.
    /// </summary>
    /// <param name="product">Product to project.</param>
    /// <param name="category">
    /// Category to take the display data from. When null, the product's loaded navigation is used.
    /// </param>
    /// <returns>The projected DTO.</returns>
    public static ProductDto ToDto(this Product product, Category? category = null)
    {
        var source = category ?? product.Category;

        return new ProductDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.CategoryId,
            source?.Code,
            source?.Name ?? string.Empty,
            product.BaseUnit,
            product.PurchaseUnit,
            product.PurchaseUnitFactor,
            product.PurchasePrice,
            product.UnitCost,
            product.SalePrice,
            product.TaxRate,
            product.CurrentStock,
            product.MinimumStock,
            product.IsActive,
            product.HasLowStock);
    }
}
