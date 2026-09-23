using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StockFlow.Application.Customers;
using StockFlow.Application.Customers.Queries;
using StockFlow.Web.Authorization;

namespace StockFlow.Web.Pages.Customers;

/// <summary>
/// Customer purchase history page. The actual history depends on the sales module, which is not
/// implemented yet, so this page shows the customer data and a placeholder for the coming feature.
/// </summary>
[Authorize(Policy = Policies.CanManageCustomers)]
public class HistoryModel : PageModel
{
    // Query handler used to load the customer whose history is displayed.
    private readonly GetCustomerByIdHandler _getCustomer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="getCustomer">Query handler that loads the customer.</param>
    public HistoryModel(GetCustomerByIdHandler getCustomer)
    {
        _getCustomer = getCustomer;
    }

    /// <summary>Customer whose purchase history is being viewed.</summary>
    public CustomerDto Customer { get; private set; } = null!;

    /// <summary>
    /// Loads the customer into the page.
    /// </summary>
    /// <param name="id">Identifier of the customer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page, or a not-found response when the customer does not exist.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _getCustomer.HandleAsync(new GetCustomerByIdQuery(id), cancellationToken);

        if (customer is null)
        {
            return NotFound();
        }

        Customer = customer;
        return Page();
    }
}
