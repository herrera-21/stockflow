using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Products;
using StockFlow.Application.Products.Queries;
using StockFlow.Application.Suppliers;
using StockFlow.Application.Suppliers.Commands;
using StockFlow.Application.Suppliers.Queries;
using StockFlow.Web.Authorization;

namespace StockFlow.Web.Pages.Suppliers;

/// <summary>
/// Page that associates products with a supplier, editing the supplier-specific data (supplier SKU,
/// purchase price and preferred flag). Only users with the manage-suppliers policy can access it.
/// </summary>
[Authorize(Policy = Policies.CanManageSuppliers)]
public class ProductsModel : PageModel
{
    // Number of products offered in the association drop-down. The app is a mini-ERP, so the whole
    // catalog fits in a single page.
    private const int ProductOptionsPageSize = 1000;

    // Use cases and localizer used to load the supplier, its products and to save changes.
    private readonly GetSupplierByIdHandler _getSupplier;
    private readonly GetSupplierProductsHandler _getSupplierProducts;
    private readonly GetProductsPagedHandler _getProducts;
    private readonly AssignSupplierProductHandler _assignProduct;
    private readonly RemoveSupplierProductHandler _removeProduct;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="getSupplier">Query handler that loads the supplier.</param>
    /// <param name="getSupplierProducts">Query handler that loads the associated products.</param>
    /// <param name="getProducts">Query handler that loads the catalog for the drop-down.</param>
    /// <param name="assignProduct">Command handler that creates or updates an association.</param>
    /// <param name="removeProduct">Command handler that removes an association.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public ProductsModel(
        GetSupplierByIdHandler getSupplier,
        GetSupplierProductsHandler getSupplierProducts,
        GetProductsPagedHandler getProducts,
        AssignSupplierProductHandler assignProduct,
        RemoveSupplierProductHandler removeProduct,
        IStringLocalizer<SharedResource> localizer)
    {
        _getSupplier = getSupplier;
        _getSupplierProducts = getSupplierProducts;
        _getProducts = getProducts;
        _assignProduct = assignProduct;
        _removeProduct = removeProduct;
        _localizer = localizer;
    }

    /// <summary>Supplier whose products are being managed.</summary>
    public SupplierDto Supplier { get; private set; } = null!;

    /// <summary>Products currently associated with the supplier.</summary>
    public IReadOnlyList<SupplierProductDto> Products { get; private set; } = [];

    /// <summary>Active products available to associate.</summary>
    public IReadOnlyList<ProductDto> AvailableProducts { get; private set; } = [];

    /// <summary>Data posted by the association form.</summary>
    [BindProperty]
    public AssignSupplierProductInputModel Input { get; set; } = new();

    /// <summary>
    /// Loads the supplier, its associated products and the catalog. When <paramref name="productId"/>
    /// is provided, the form is prefilled to edit that association.
    /// </summary>
    /// <param name="id">Identifier of the supplier.</param>
    /// <param name="productId">Optional product whose association is being edited.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page, or a not-found response when the supplier does not exist.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, Guid? productId, CancellationToken cancellationToken)
    {
        var supplier = await _getSupplier.HandleAsync(new GetSupplierByIdQuery(id), cancellationToken);

        if (supplier is null)
        {
            return NotFound();
        }

        Supplier = supplier;
        await LoadAsync(id, cancellationToken);

        if (productId is { } selectedId)
        {
            var association = Products.FirstOrDefault(p => p.ProductId == selectedId);
            if (association is not null)
            {
                Input = new AssignSupplierProductInputModel
                {
                    ProductId = association.ProductId,
                    SupplierSku = association.SupplierSku,
                    PurchasePrice = association.PurchasePrice,
                    IsPreferred = association.IsPreferred
                };
            }
        }

        return Page();
    }

    /// <summary>
    /// Validates the form and associates the product with the supplier, creating or updating the
    /// association.
    /// </summary>
    /// <param name="id">Identifier of the supplier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A redirect to the page on success, or the page with validation errors.</returns>
    public async Task<IActionResult> OnPostAssignAsync(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _getSupplier.HandleAsync(new GetSupplierByIdQuery(id), cancellationToken);

        if (supplier is null)
        {
            return NotFound();
        }

        Supplier = supplier;

        if (!ModelState.IsValid)
        {
            await LoadAsync(id, cancellationToken);
            return Page();
        }

        var command = new AssignSupplierProductCommand(
            id,
            Input.ProductId,
            Input.SupplierSku,
            Input.PurchasePrice,
            Input.IsPreferred);

        var result = await _assignProduct.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.Error!]);
            await LoadAsync(id, cancellationToken);
            return Page();
        }

        TempData["StatusMessage"] = _localizer["SupplierProductAssigned"].Value;
        return RedirectToPage("Products", new { id });
    }

    /// <summary>
    /// Removes the association between the supplier and a product.
    /// </summary>
    /// <param name="id">Identifier of the supplier.</param>
    /// <param name="productId">Identifier of the product to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A redirect back to the page.</returns>
    public async Task<IActionResult> OnPostRemoveAsync(Guid id, Guid productId, CancellationToken cancellationToken)
    {
        var supplier = await _getSupplier.HandleAsync(new GetSupplierByIdQuery(id), cancellationToken);

        if (supplier is null)
        {
            return NotFound();
        }

        var result = await _removeProduct.HandleAsync(new RemoveSupplierProductCommand(id, productId), cancellationToken);

        if (result.IsSuccess)
        {
            TempData["StatusMessage"] = _localizer["SupplierProductRemoved"].Value;
        }
        else
        {
            TempData["ErrorMessage"] = _localizer[result.Error!].Value;
        }

        return RedirectToPage("Products", new { id });
    }

    // Loads the associated products and the active catalog for the drop-down.
    private async Task LoadAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        Products = await _getSupplierProducts.HandleAsync(new GetSupplierProductsQuery(supplierId), cancellationToken);

        var catalog = await _getProducts.HandleAsync(
            new GetProductsPagedQuery(1, ProductOptionsPageSize),
            cancellationToken);

        AvailableProducts = catalog.Items.Where(p => p.IsActive).ToList();
    }
}
