using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Categories.Queries;
using StockFlow.Application.Common.Models;
using StockFlow.Application.Products;
using StockFlow.Application.Products.Commands;
using StockFlow.Application.Products.Queries;
using StockFlow.Web.Authorization;
using StockFlow.Web.Localization;

namespace StockFlow.Web.Pages.Products;

/// <summary>
/// Product listing page with live search, category filter and htmx-powered row updates.
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    /// <summary>Number of products shown per page.</summary>
    public const int PageSize = 10;

    // Use cases and services used to render the list and process deactivation.
    private readonly GetProductsPagedHandler _getProducts;
    private readonly GetCategoriesHandler _getCategories;
    private readonly DeactivateProductHandler _deactivateProduct;
    private readonly IAuthorizationService _authorizationService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="getProducts">Query handler that loads the page of products.</param>
    /// <param name="getCategories">Query handler that loads the active categories.</param>
    /// <param name="deactivateProduct">Command handler that soft-deletes a product.</param>
    /// <param name="authorizationService">Service used to check the manage-products policy.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public IndexModel(
        GetProductsPagedHandler getProducts,
        GetCategoriesHandler getCategories,
        DeactivateProductHandler deactivateProduct,
        IAuthorizationService authorizationService,
        IStringLocalizer<SharedResource> localizer)
    {
        _getProducts = getProducts;
        _getCategories = getCategories;
        _deactivateProduct = deactivateProduct;
        _authorizationService = authorizationService;
        _localizer = localizer;
    }

    /// <summary>Page of products to render.</summary>
    public PagedResult<ProductDto> Products { get; private set; } = new([], 1, PageSize, 0);

    /// <summary>True when the current user may manage products.</summary>
    public bool CanManageProducts { get; private set; }

    /// <summary>True when the current user may adjust stock.</summary>
    public bool CanAdjustInventory { get; private set; }

    /// <summary>Active categories offered by the filter.</summary>
    public IReadOnlyList<SelectListItem> Categories { get; private set; } = [];

    /// <summary>Current page number, bound from the query string.</summary>
    [BindProperty(SupportsGet = true, Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    /// <summary>Optional search text, bound from the query string.</summary>
    [BindProperty(SupportsGet = true, Name = "search")]
    public string? Search { get; set; }

    /// <summary>Optional category filter, bound from the query string.</summary>
    [BindProperty(SupportsGet = true, Name = "categoryId")]
    public Guid? CategoryId { get; set; }

    /// <summary>Handles GET requests and loads the requested page of products.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    /// <summary>
    /// htmx endpoint that returns only the results container so the page is not reloaded. Because
    /// the search inputs do not send "page", a new search always restarts at page 1.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The partial view with the product rows.</returns>
    public async Task<IActionResult> OnGetRowsAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        return Partial("_ProductsTable", this);
    }

    /// <summary>
    /// Deactivates a product. Responds with the updated row for htmx requests, or redirects back to
    /// the list otherwise.
    /// </summary>
    /// <param name="id">Identifier of the product to deactivate.</param>
    /// <param name="pageNumber">Current page number, preserved across the redirect.</param>
    /// <param name="search">Current search text, preserved across the redirect.</param>
    /// <param name="categoryId">Current category filter, preserved across the redirect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated row, or a redirect back to the list.</returns>
    public async Task<IActionResult> OnPostDeactivateAsync(
        Guid id,
        int pageNumber,
        string? search,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        if (!await CanManageAsync())
        {
            return Forbid();
        }

        var result = await _deactivateProduct.HandleAsync(new DeactivateProductCommand(id), cancellationToken);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            // htmx swaps the row in place. Client-side authorization is not security: the policy
            // check above already ran on the server.
            if (!result.IsSuccess)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return new EmptyResult();
            }

            return Partial("_ProductRow", new ProductRowViewModel(
                result.Value!,
                CanManageProducts: true,
                await CanAdjustAsync(),
                pageNumber,
                search,
                categoryId));
        }

        if (result.IsSuccess)
        {
            TempData["StatusMessage"] = _localizer["ProductDeactivated"].Value;
        }
        else
        {
            TempData["ErrorMessage"] = _localizer[result.Error!].Value;
        }

        return RedirectToPage("Index", new { pageNumber, search, categoryId });
    }

    // Loads the page of products, the manage-products flag and the category filter options.
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        CanManageProducts = await CanManageAsync();
        CanAdjustInventory = await CanAdjustAsync();
        Products = await _getProducts.HandleAsync(
            new GetProductsPagedQuery(PageNumber, PageSize, Search, CategoryId),
            cancellationToken);

        var categories = await _getCategories.HandleAsync(new GetCategoriesQuery(), cancellationToken);
        Categories = _localizer.ToCategorySelectList(categories);
    }

    // Checks whether the current user satisfies the manage-products policy.
    private async Task<bool> CanManageAsync() =>
        (await _authorizationService.AuthorizeAsync(User, Policies.CanManageProducts)).Succeeded;

    // Checks whether the current user satisfies the adjust-inventory policy.
    private async Task<bool> CanAdjustAsync() =>
        (await _authorizationService.AuthorizeAsync(User, Policies.CanAdjustInventory)).Succeeded;
}
