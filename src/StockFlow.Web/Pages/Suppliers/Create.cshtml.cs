using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Suppliers.Commands;
using StockFlow.Web.Authorization;
using StockFlow.Web.Localization;

namespace StockFlow.Web.Pages.Suppliers;

/// <summary>
/// Create supplier page. Only users with the manage-suppliers policy can access it.
/// </summary>
[Authorize(Policy = Policies.CanManageSuppliers)]
public class CreateModel : PageModel
{
    // Use case and localizer used to create the supplier and report errors.
    private readonly CreateSupplierHandler _createSupplier;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="createSupplier">Command handler that creates the supplier.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public CreateModel(CreateSupplierHandler createSupplier, IStringLocalizer<SharedResource> localizer)
    {
        _createSupplier = createSupplier;
        _localizer = localizer;
    }

    /// <summary>Data posted by the create form.</summary>
    [BindProperty]
    public CreateSupplierInputModel Input { get; set; } = new();

    /// <summary>Identity document types offered by the form.</summary>
    public IReadOnlyList<SelectListItem> DocumentTypes { get; private set; } = [];

    /// <summary>Handles GET requests and loads the document types.</summary>
    public void OnGet()
    {
        DocumentTypes = _localizer.ToDocumentTypeSelectList();
    }

    /// <summary>
    /// Validates the form and creates the supplier.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A redirect to the list on success, or the page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            DocumentTypes = _localizer.ToDocumentTypeSelectList();
            return Page();
        }

        var command = new CreateSupplierCommand(
            Input.Name,
            Input.DocumentType!.Value,
            Input.TaxId,
            Input.ContactName,
            Input.Phone,
            Input.Email,
            Input.Address);

        var result = await _createSupplier.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("Input.TaxId", _localizer[result.Error!]);
            DocumentTypes = _localizer.ToDocumentTypeSelectList();
            return Page();
        }

        TempData["StatusMessage"] = _localizer["SupplierCreated"].Value;
        return RedirectToPage("Index");
    }
}
