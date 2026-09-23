using StockFlow.Application.Customers;

namespace StockFlow.Web.Pages.Customers;

/// <summary>
/// Row-level view model so the same partial can be rendered from the full page and from an htmx
/// response after deactivating a customer.
/// </summary>
/// <param name="Customer">Customer shown in the row.</param>
/// <param name="PageNumber">Current page number, preserved by the row actions.</param>
/// <param name="Search">Current search text, preserved by the row actions.</param>
public record CustomerRowViewModel(
    CustomerDto Customer,
    int PageNumber,
    string? Search);
