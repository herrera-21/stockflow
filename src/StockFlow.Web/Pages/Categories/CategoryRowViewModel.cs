using StockFlow.Application.Categories;

namespace StockFlow.Web.Pages.Categories;

/// <summary>
/// Row-level view model so the same partial can be rendered from the full page and from an htmx
/// response after deactivating or reactivating a category.
/// </summary>
/// <param name="Category">Category shown in the row.</param>
/// <param name="PageNumber">Current page number, preserved by the row actions.</param>
/// <param name="Search">Current search text, preserved by the row actions.</param>
public record CategoryRowViewModel(
    CategoryDto Category,
    int PageNumber,
    string? Search);
