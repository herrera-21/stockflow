using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Common.Models;
using StockFlow.Application.Customers;
using StockFlow.Application.Customers.Commands;
using StockFlow.Application.Customers.Queries;
using StockFlow.Web.Authorization;

namespace StockFlow.Web.Pages.Customers;

/// <summary>
/// Customer listing page with live search and htmx-powered row updates.
/// </summary>
[Authorize(Policy = Policies.CanManageCustomers)]
public class IndexModel : PageModel
{
    /// <summary>Number of customers shown per page.</summary>
    public const int PageSize = 10;

    // Use cases used to render the list and process deactivation.
    private readonly GetCustomersPagedHandler _getCustomers;
    private readonly DeactivateCustomerHandler _deactivateCustomer;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="getCustomers">Query handler that loads the page of customers.</param>
    /// <param name="deactivateCustomer">Command handler that soft-deletes a customer.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public IndexModel(
        GetCustomersPagedHandler getCustomers,
        DeactivateCustomerHandler deactivateCustomer,
        IStringLocalizer<SharedResource> localizer)
    {
        _getCustomers = getCustomers;
        _deactivateCustomer = deactivateCustomer;
        _localizer = localizer;
    }

    /// <summary>Page of customers to render.</summary>
    public PagedResult<CustomerDto> Customers { get; private set; } = new([], 1, PageSize, 0);

    /// <summary>Current page number, bound from the query string.</summary>
    [BindProperty(SupportsGet = true, Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    /// <summary>Optional search text, bound from the query string.</summary>
    [BindProperty(SupportsGet = true, Name = "search")]
    public string? Search { get; set; }

    /// <summary>Handles GET requests and loads the requested page of customers.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    /// <summary>
    /// htmx endpoint that returns only the results container so the page is not reloaded. Because
    /// the search input does not send "page", a new search always restarts at page 1.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The partial view with the customer rows.</returns>
    public async Task<IActionResult> OnGetRowsAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        return Partial("_CustomersTable", this);
    }

    /// <summary>
    /// Deactivates a customer. Responds with the updated row for htmx requests, or redirects back to
    /// the list otherwise.
    /// </summary>
    /// <param name="id">Identifier of the customer to deactivate.</param>
    /// <param name="pageNumber">Current page number, preserved across the redirect.</param>
    /// <param name="search">Current search text, preserved across the redirect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated row, or a redirect back to the list.</returns>
    public async Task<IActionResult> OnPostDeactivateAsync(
        Guid id,
        int pageNumber,
        string? search,
        CancellationToken cancellationToken)
    {
        var result = await _deactivateCustomer.HandleAsync(new DeactivateCustomerCommand(id), cancellationToken);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            // htmx swaps the row in place. Authorization is enforced by the page policy above.
            if (!result.IsSuccess)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return new EmptyResult();
            }

            return Partial("_CustomerRow", new CustomerRowViewModel(result.Value!, pageNumber, search));
        }

        if (result.IsSuccess)
        {
            TempData["StatusMessage"] = _localizer["CustomerDeactivated"].Value;
        }
        else
        {
            TempData["ErrorMessage"] = _localizer[result.Error!].Value;
        }

        return RedirectToPage("Index", new { pageNumber, search });
    }

    // Loads the page of customers.
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Customers = await _getCustomers.HandleAsync(
            new GetCustomersPagedQuery(PageNumber, PageSize, Search),
            cancellationToken);
    }
}
