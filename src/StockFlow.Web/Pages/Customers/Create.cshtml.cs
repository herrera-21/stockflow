using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Customers.Commands;
using StockFlow.Web.Authorization;

namespace StockFlow.Web.Pages.Customers;

/// <summary>
/// Create customer page. Only users with the manage-customers policy can access it.
/// </summary>
[Authorize(Policy = Policies.CanManageCustomers)]
public class CreateModel : PageModel
{
    // Use case and localizer used to create the customer and report errors.
    private readonly CreateCustomerHandler _createCustomer;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="createCustomer">Command handler that creates the customer.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public CreateModel(CreateCustomerHandler createCustomer, IStringLocalizer<SharedResource> localizer)
    {
        _createCustomer = createCustomer;
        _localizer = localizer;
    }

    /// <summary>Data posted by the create form.</summary>
    [BindProperty]
    public CreateCustomerInputModel Input { get; set; } = new();

    /// <summary>Handles GET requests.</summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Validates the form and creates the customer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A redirect to the list on success, or the page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var command = new CreateCustomerCommand(
            Input.Name,
            Input.TaxId,
            Input.Phone,
            Input.Email,
            Input.Address);

        var result = await _createCustomer.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("Input.TaxId", _localizer[result.Error!]);
            return Page();
        }

        TempData["StatusMessage"] = _localizer["CustomerCreated"].Value;
        return RedirectToPage("Index");
    }
}
