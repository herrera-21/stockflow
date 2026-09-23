using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Customers;
using StockFlow.Application.Customers.Commands;
using StockFlow.Application.Customers.Queries;
using StockFlow.Web.Authorization;

namespace StockFlow.Web.Pages.Customers;

/// <summary>
/// Edit customer page. Only users with the manage-customers policy can access it.
/// </summary>
[Authorize(Policy = Policies.CanManageCustomers)]
public class EditModel : PageModel
{
    // Use cases and localizer used to load and update the customer.
    private readonly GetCustomerByIdHandler _getCustomer;
    private readonly UpdateCustomerHandler _updateCustomer;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="getCustomer">Query handler that loads the customer.</param>
    /// <param name="updateCustomer">Command handler that updates the customer.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public EditModel(
        GetCustomerByIdHandler getCustomer,
        UpdateCustomerHandler updateCustomer,
        IStringLocalizer<SharedResource> localizer)
    {
        _getCustomer = getCustomer;
        _updateCustomer = updateCustomer;
        _localizer = localizer;
    }

    /// <summary>Data posted by the edit form.</summary>
    [BindProperty]
    public UpdateCustomerInputModel Input { get; set; } = new();

    /// <summary>
    /// Loads the customer into the form.
    /// </summary>
    /// <param name="id">Identifier of the customer to edit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page, or a not-found response when the customer does not exist.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _getCustomer.HandleAsync(new GetCustomerByIdQuery(id), cancellationToken);

        if (customer is null)
        {
            return NotFound();
        }

        Input = new UpdateCustomerInputModel
        {
            Id = customer.Id,
            Name = customer.Name,
            TaxId = customer.TaxId,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address
        };

        return Page();
    }

    /// <summary>
    /// Validates the form and updates the customer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A redirect to the list on success, or the page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var command = new UpdateCustomerCommand(
            Input.Id,
            Input.Name,
            Input.TaxId,
            Input.Phone,
            Input.Email,
            Input.Address);

        var result = await _updateCustomer.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == CustomerErrorCodes.NotFound)
            {
                return NotFound();
            }

            ModelState.AddModelError("Input.TaxId", _localizer[result.Error!]);
            return Page();
        }

        TempData["StatusMessage"] = _localizer["CustomerUpdated"].Value;
        return RedirectToPage("Index");
    }
}
