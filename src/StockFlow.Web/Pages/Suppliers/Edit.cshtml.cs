using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Suppliers;
using StockFlow.Application.Suppliers.Commands;
using StockFlow.Application.Suppliers.Queries;
using StockFlow.Web.Authorization;

namespace StockFlow.Web.Pages.Suppliers;

/// <summary>
/// Edit supplier page. Only users with the manage-suppliers policy can access it.
/// </summary>
[Authorize(Policy = Policies.CanManageSuppliers)]
public class EditModel : PageModel
{
    // Use cases and localizer used to load and update the supplier.
    private readonly GetSupplierByIdHandler _getSupplier;
    private readonly UpdateSupplierHandler _updateSupplier;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="getSupplier">Query handler that loads the supplier.</param>
    /// <param name="updateSupplier">Command handler that updates the supplier.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public EditModel(
        GetSupplierByIdHandler getSupplier,
        UpdateSupplierHandler updateSupplier,
        IStringLocalizer<SharedResource> localizer)
    {
        _getSupplier = getSupplier;
        _updateSupplier = updateSupplier;
        _localizer = localizer;
    }

    /// <summary>Data posted by the edit form.</summary>
    [BindProperty]
    public UpdateSupplierInputModel Input { get; set; } = new();

    /// <summary>
    /// Loads the supplier into the form.
    /// </summary>
    /// <param name="id">Identifier of the supplier to edit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page, or a not-found response when the supplier does not exist.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _getSupplier.HandleAsync(new GetSupplierByIdQuery(id), cancellationToken);

        if (supplier is null)
        {
            return NotFound();
        }

        Input = new UpdateSupplierInputModel
        {
            Id = supplier.Id,
            Name = supplier.Name,
            TaxId = supplier.TaxId,
            ContactName = supplier.ContactName,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address
        };

        return Page();
    }

    /// <summary>
    /// Validates the form and updates the supplier.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A redirect to the list on success, or the page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var command = new UpdateSupplierCommand(
            Input.Id,
            Input.Name,
            Input.TaxId,
            Input.ContactName,
            Input.Phone,
            Input.Email,
            Input.Address);

        var result = await _updateSupplier.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == SupplierErrorCodes.NotFound)
            {
                return NotFound();
            }

            ModelState.AddModelError("Input.TaxId", _localizer[result.Error!]);
            return Page();
        }

        TempData["StatusMessage"] = _localizer["SupplierUpdated"].Value;
        return RedirectToPage("Index");
    }
}
