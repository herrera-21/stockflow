using StockFlow.Application.Suppliers;

namespace StockFlow.Web.Pages.Suppliers;

/// <summary>
/// Row-level view model so the same partial can be rendered from the full page and from an htmx
/// response after deactivating a supplier.
/// </summary>
/// <param name="Supplier">Supplier shown in the row.</param>
/// <param name="PageNumber">Current page number, preserved by the row actions.</param>
/// <param name="Search">Current search text, preserved by the row actions.</param>
public record SupplierRowViewModel(
    SupplierDto Supplier,
    int PageNumber,
    string? Search);
