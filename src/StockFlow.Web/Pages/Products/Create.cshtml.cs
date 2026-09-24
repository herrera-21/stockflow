using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Categories.Queries;
using StockFlow.Application.Products;
using StockFlow.Application.Products.Commands;
using StockFlow.Domain;
using StockFlow.Web.Authorization;
using StockFlow.Web.Localization;

namespace StockFlow.Web.Pages.Products;

/// <summary>
/// Create product page. Only users with the manage-products policy can access it.
/// </summary>
[Authorize(Policy = Policies.CanManageProducts)]
public class CreateModel : PageModel
{
    // Use cases and localizer used to create the product and report errors.
    private readonly CreateProductHandler _createProduct;
    private readonly GetCategoriesHandler _getCategories;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="createProduct">Command handler that creates the product.</param>
    /// <param name="getCategories">Query handler that loads the active categories.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public CreateModel(
        CreateProductHandler createProduct,
        GetCategoriesHandler getCategories,
        IStringLocalizer<SharedResource> localizer)
    {
        _createProduct = createProduct;
        _getCategories = getCategories;
        _localizer = localizer;
    }

    /// <summary>Data posted by the create form.</summary>
    [BindProperty]
    public CreateProductInputModel Input { get; set; } = new();

    /// <summary>Active categories offered by the form.</summary>
    public IReadOnlyList<SelectListItem> Categories { get; private set; } = [];

    /// <summary>Units of measure offered by the base and purchase unit selectors.</summary>
    public IReadOnlyList<SelectListItem> Units { get; private set; } = [];

    /// <summary>Handles GET requests and pre-fills the form defaults.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // Default tax rate (13%) is a convenience, not a business rule.
        Input.TaxRate = 13m;
        Input.BaseUnit = UnitOfMeasure.Unit;
        Input.PurchaseUnit = UnitOfMeasure.Unit;
        Input.PurchaseUnitFactor = 1m;
        await LoadOptionsAsync(cancellationToken);
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
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }

        var command = new CreateProductCommand(
            Input.Sku,
            Input.Name,
            Input.Description,
            Input.CategoryId!.Value,
            Input.BaseUnit!.Value,
            Input.PurchaseUnit!.Value,
            Input.PurchaseUnitFactor!.Value,
            Input.PurchasePrice!.Value,
            Input.SalePrice!.Value,
            Input.TaxRate!.Value,
            Input.InitialStock!.Value,
            Input.MinimumStock!.Value);

        var result = await _createProduct.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var field = result.Error == ProductErrorCodes.CategoryNotFound ? "Input.CategoryId" : "Input.Sku";
            ModelState.AddModelError(field, _localizer[result.Error!]);
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }

        TempData["StatusMessage"] = _localizer["ProductCreated"].Value;
        return RedirectToPage("Index");
    }

    // Loads the active categories and the units of measure used by the selectors.
    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        var categories = await _getCategories.HandleAsync(new GetCategoriesQuery(), cancellationToken);
        Categories = _localizer.ToCategorySelectList(categories);
        Units = _localizer.ToUnitOfMeasureSelectList();
    }
}
