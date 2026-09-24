using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Inventory;
using StockFlow.Application.Inventory.Commands;
using StockFlow.Application.Products;
using StockFlow.Application.Products.Queries;
using StockFlow.Domain;
using StockFlow.Web.Authorization;
using StockFlow.Web.Localization;

namespace StockFlow.Web.Pages.Products;

/// <summary>
/// Stock adjustment page: records a manual movement and shows the resulting stock ahead of saving.
/// Only users with the adjust-inventory policy can access it.
/// </summary>
[Authorize(Policy = Policies.CanAdjustInventory)]
public class AdjustModel : PageModel
{
    // Use cases and localizer used to load the product, record the movement and report errors.
    private readonly GetProductByIdHandler _getProduct;
    private readonly AdjustStockHandler _adjustStock;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="getProduct">Query handler that loads the product.</param>
    /// <param name="adjustStock">Command handler that records the adjustment.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public AdjustModel(
        GetProductByIdHandler getProduct,
        AdjustStockHandler adjustStock,
        IStringLocalizer<SharedResource> localizer)
    {
        _getProduct = getProduct;
        _adjustStock = adjustStock;
        _localizer = localizer;
    }

    /// <summary>Data posted by the adjustment form.</summary>
    [BindProperty]
    public AdjustStockInputModel Input { get; set; } = new();

    /// <summary>Product being adjusted.</summary>
    public ProductDto Product { get; private set; } = null!;

    /// <summary>Adjustment directions offered by the form.</summary>
    public IReadOnlyList<SelectListItem> Types { get; private set; } = [];

    /// <summary>Adjustment reasons offered by the form.</summary>
    public IReadOnlyList<SelectListItem> Reasons { get; private set; } = [];

    /// <summary>
    /// Loads the product and pre-fills the form with a positive adjustment.
    /// </summary>
    /// <param name="id">Identifier of the product to adjust.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page, or a not-found response when the product does not exist.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _getProduct.HandleAsync(new GetProductByIdQuery(id), cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        Product = product;
        Input.ProductId = product.Id;
        Input.Type = InventoryMovementType.AdjustmentIncrease;
        Input.Reason = InventoryAdjustmentReason.PhysicalCount;
        LoadOptions();

        return Page();
    }

    /// <summary>
    /// Validates the form and records the adjustment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A redirect to the movement history on success, or the page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Input.ProductId is not Guid productId)
        {
            return NotFound();
        }

        var product = await _getProduct.HandleAsync(new GetProductByIdQuery(productId), cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        Product = product;
        LoadOptions();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // The handler validates the resulting stock against fresh data; its InsufficientStock error
        // is mapped to the quantity field below.
        var result = await _adjustStock.HandleAsync(
            new AdjustStockCommand(
                productId,
                Input.Type!.Value,
                Input.Quantity!.Value,
                Input.Reason,
                Input.Note),
            cancellationToken);

        if (!result.IsSuccess)
        {
            var field = result.Error switch
            {
                InventoryErrorCodes.InsufficientStock or InventoryErrorCodes.InvalidQuantity => "Input.Quantity",
                InventoryErrorCodes.AdjustmentReasonRequired => "Input.Reason",
                InventoryErrorCodes.AdjustmentNoteRequired => "Input.Note",
                _ => string.Empty
            };

            ModelState.AddModelError(field, _localizer[result.Error!]);
            return Page();
        }

        TempData["StatusMessage"] = _localizer["StockAdjusted"].Value;
        return RedirectToPage("Movements", new { id = productId });
    }

    // Loads the adjustment direction and reason options.
    private void LoadOptions()
    {
        Types = _localizer.ToAdjustmentTypeSelectList();
        Reasons = _localizer.ToAdjustmentReasonSelectList();
    }
}
