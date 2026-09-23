using StockFlow.Application.Products;

namespace StockFlow.Web.Pages.Products;

/// <summary>
/// Row-level view model so the same partial can be rendered from the full page and from an htmx
/// response after deactivating a product.
/// </summary>
/// <param name="Product">Product shown in the row.</param>
/// <param name="CanManageProducts">Whether the current user may manage products.</param>
/// <param name="PageNumber">Current page number, preserved by the row actions.</param>
/// <param name="Search">Current search text, preserved by the row actions.</param>
/// <param name="Category">Current category filter, preserved by the row actions.</param>
public record ProductRowViewModel(
    ProductDto Product,
    bool CanManageProducts,
    int PageNumber,
    string? Search,
    string? Category);
