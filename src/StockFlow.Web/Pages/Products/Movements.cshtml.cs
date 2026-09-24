using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Common.Models;
using StockFlow.Application.Inventory;
using StockFlow.Application.Inventory.Queries;
using StockFlow.Application.Products;
using StockFlow.Application.Products.Queries;
using StockFlow.Web.Authorization;

namespace StockFlow.Web.Pages.Products;

/// <summary>
/// Movement history page for a single product, paginated with htmx. Every authenticated user that can
/// see the catalog can view it; adjusting still requires the inventory policy.
/// </summary>
[Authorize]
public class MovementsModel : PageModel
{
    /// <summary>Number of movements shown per page.</summary>
    public const int PageSize = 10;

    // Use cases and services used to load the product, its movements and the policy check.
    private readonly GetProductByIdHandler _getProduct;
    private readonly GetMovementsPagedHandler _getMovements;
    private readonly IAuthorizationService _authorizationService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="getProduct">Query handler that loads the product.</param>
    /// <param name="getMovements">Query handler that loads the page of movements.</param>
    /// <param name="authorizationService">Service used to check the adjust-inventory policy.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public MovementsModel(
        GetProductByIdHandler getProduct,
        GetMovementsPagedHandler getMovements,
        IAuthorizationService authorizationService,
        IStringLocalizer<SharedResource> localizer)
    {
        _getProduct = getProduct;
        _getMovements = getMovements;
        _authorizationService = authorizationService;
        _localizer = localizer;
    }

    /// <summary>Product whose history is shown.</summary>
    public ProductDto Product { get; private set; } = null!;

    /// <summary>Page of movements to render.</summary>
    public PagedResult<InventoryMovementDto> Movements { get; private set; } = new([], 1, PageSize, 0);

    /// <summary>True when the current user may adjust stock.</summary>
    public bool CanAdjustInventory { get; private set; }

    /// <summary>Current page number, bound from the query string.</summary>
    [BindProperty(SupportsGet = true, Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Loads the product and the requested page of movements.
    /// </summary>
    /// <param name="id">Identifier of the product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page, or a not-found response when the product does not exist.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        return Page();
    }

    /// <summary>
    /// htmx endpoint that returns only the movement rows so paging does not reload the page.
    /// </summary>
    /// <param name="id">Identifier of the product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The partial view with the movement rows.</returns>
    public async Task<IActionResult> OnGetRowsAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        return Partial("_MovementsTable", this);
    }

    // Loads the product, its movements page and the adjust-inventory flag.
    private async Task<bool> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _getProduct.HandleAsync(new GetProductByIdQuery(id), cancellationToken);

        if (product is null)
        {
            return false;
        }

        Product = product;
        Movements = await _getMovements.HandleAsync(
            new GetMovementsPagedQuery(id, PageNumber, PageSize),
            cancellationToken);
        CanAdjustInventory = (await _authorizationService.AuthorizeAsync(User, Policies.CanAdjustInventory)).Succeeded;

        return true;
    }
}
