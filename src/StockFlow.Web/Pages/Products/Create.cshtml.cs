using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Products.Commands;
using StockFlow.Web.Authorization;

namespace StockFlow.Web.Pages.Products;

/// <summary>
/// Create product page. Only users with the manage-products policy can access it.
/// </summary>
[Authorize(Policy = Policies.CanManageProducts)]
public class CreateModel : PageModel
{
    // Use case and localizer used to create the product and report errors.
    private readonly CreateProductHandler _createProduct;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="createProduct">Command handler that creates the product.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public CreateModel(CreateProductHandler createProduct, IStringLocalizer<SharedResource> localizer)
    {
        _createProduct = createProduct;
        _localizer = localizer;
    }

    /// <summary>Data posted by the create form.</summary>
    [BindProperty]
    public CreateProductInputModel Input { get; set; } = new();

    /// <summary>Handles GET requests and pre-fills the form defaults.</summary>
    public void OnGet()
    {
        // Default tax rate (13%) is a convenience, not a business rule.
        Input.TaxRate = 13m;
    }

    /// <summary>
    /// Validates the form and creates the product.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A redirect to the list on success, or the page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var command = new CreateProductCommand(
            Input.Sku,
            Input.Name,
            Input.Description,
            Input.Category,
            Input.PurchasePrice,
            Input.SalePrice,
            Input.TaxRate,
            Input.InitialStock,
            Input.MinimumStock);

        var result = await _createProduct.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("Input.Sku", _localizer[result.Error!]);
            return Page();
        }

        TempData["StatusMessage"] = _localizer["ProductCreated"].Value;
        return RedirectToPage("Index");
    }
}
