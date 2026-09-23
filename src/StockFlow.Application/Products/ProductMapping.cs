using StockFlow.Domain.Entities;

namespace StockFlow.Application.Products;

/// <summary>
/// Maps product domain entities to their DTO representation.
/// </summary>
internal static class ProductMapping
{
    /// <summary>Projects a <see cref="Product"/> into a <see cref="ProductDto"/>.</summary>
    /// <param name="product">Product to project.</param>
    /// <returns>The projected DTO.</returns>
    public static ProductDto ToDto(this Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Description,
        product.Category,
        product.PurchasePrice,
        product.SalePrice,
        product.TaxRate,
        product.CurrentStock,
        product.MinimumStock,
        product.IsActive,
        product.HasLowStock);
}
