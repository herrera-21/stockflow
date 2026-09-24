using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Categories.Queries;
using StockFlow.Application.Products;
using StockFlow.Application.Products.Commands;
using StockFlow.Application.Products.Queries;
using StockFlow.Web.Authorization;
using StockFlow.Web.Localization;

namespace StockFlow.Web.Pages.Products;

/// <summary>
/// Edit product page. Only users with the manage-products policy can access it.
/// </summary>
[Authorize(Policy = Policies.CanManageProducts)]
public class EditModel : PageModel
{
    // Use cases and localizer used to load and update the product.
    private readonly GetProductByIdHandler _getProduct;
    private readonly UpdateProductHandler _updateProduct;
    private readonly GetCategoriesHandler _getCategories;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="getProduct">Query handler that loads the product.</param>
    /// <param name="updateProduct">Command handler that updates the product.</param>
    /// <param name="getCategories">Query handler that loads the active categories.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public EditModel(
        GetProductByIdHandler getProduct,
        UpdateProductHandler updateProduct,
        GetCategoriesHandler getCategories,
        IStringLocalizer<SharedResource> localizer)
    {
        _getProduct = getProduct;
        _updateProduct = updateProduct;
        _getCategories = getCategories;
        _localizer = localizer;
    }

    /// <summary>Data posted by the edit form.</summary>
    [BindProperty]
    public UpdateProductInputModel Input { get; set; } = new();

    /// <summary>Active categories offered by the form.</summary>
    public IReadOnlyList<SelectListItem> Categories { get; private set; } = [];

    /// <summary>Units of measure offered by the base and purchase unit selectors.</summary>
    public IReadOnlyList<SelectListItem> Units { get; private set; } = [];

    /// <summary>
    /// Loads the product into the form.
    /// </summary>
    /// <param name="id">Identifier of the product to edit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page, or a not-found response when the product does not exist.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _getProduct.HandleAsync(new GetProductByIdQuery(id), cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        Input = new UpdateProductInputModel
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            CategoryId = product.CategoryId,
            BaseUnit = product.BaseUnit,
            PurchaseUnit = product.PurchaseUnit,
            PurchaseUnitFactor = product.PurchaseUnitFactor,
            PurchasePrice = product.PurchasePrice,
            SalePrice = product.SalePrice,
            TaxRate = product.TaxRate,
            MinimumStock = product.MinimumStock
        };

        await LoadOptionsAsync(cancellationToken);

        return Page();
    }

    /// <summary>
    /// Validates the form and updates the product.
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

        var command = new UpdateProductCommand(
            Input.Id,
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
            Input.MinimumStock!.Value);

        var result = await _updateProduct.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == ProductErrorCodes.NotFound)
            {
                return NotFound();
            }

            var field = result.Error switch
            {
                ProductErrorCodes.CategoryNotFound => "Input.CategoryId",
                ProductErrorCodes.BaseUnitLocked => "Input.BaseUnit",
                _ => "Input.Sku"
            };
            ModelState.AddModelError(field, _localizer[result.Error!]);
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }

        TempData["StatusMessage"] = _localizer["ProductUpdated"].Value;
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
