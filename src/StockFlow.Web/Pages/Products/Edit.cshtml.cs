using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Products;
using StockFlow.Application.Products.Commands;
using StockFlow.Application.Products.Queries;
using StockFlow.Web.Authorization;

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
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="getProduct">Query handler that loads the product.</param>
    /// <param name="updateProduct">Command handler that updates the product.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public EditModel(
        GetProductByIdHandler getProduct,
        UpdateProductHandler updateProduct,
        IStringLocalizer<SharedResource> localizer)
    {
        _getProduct = getProduct;
        _updateProduct = updateProduct;
        _localizer = localizer;
    }

    /// <summary>Data posted by the edit form.</summary>
    [BindProperty]
    public UpdateProductInputModel Input { get; set; } = new();

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
            Category = product.Category,
            PurchasePrice = product.PurchasePrice,
            SalePrice = product.SalePrice,
            TaxRate = product.TaxRate,
            MinimumStock = product.MinimumStock
        };

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
            return Page();
        }

        var command = new UpdateProductCommand(
            Input.Id,
            Input.Sku,
            Input.Name,
            Input.Description,
            Input.Category,
            Input.PurchasePrice,
            Input.SalePrice,
            Input.TaxRate,
            Input.MinimumStock);

        var result = await _updateProduct.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == ProductErrorCodes.NotFound)
            {
                return NotFound();
            }

            ModelState.AddModelError("Input.Sku", _localizer[result.Error!]);
            return Page();
        }

        TempData["StatusMessage"] = _localizer["ProductUpdated"].Value;
        return RedirectToPage("Index");
    }
}
