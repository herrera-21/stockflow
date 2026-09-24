using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Categories;
using StockFlow.Application.Categories.Commands;
using StockFlow.Application.Categories.Queries;
using StockFlow.Application.Common.Models;
using StockFlow.Web.Authorization;

namespace StockFlow.Web.Pages.Categories;

/// <summary>
/// Category listing page with live search and htmx-powered search. Deactivate and activate are full
/// posts so their outcome (including the "in use" refusal) can be reported with a flash message.
/// Only administrators can access it.
/// </summary>
[Authorize(Policy = Policies.CanManageCategories)]
public class IndexModel : PageModel
{
    /// <summary>Number of categories shown per page.</summary>
    public const int PageSize = 10;

    // Use cases used to render the list and change category state.
    private readonly GetCategoriesPagedHandler _getCategories;
    private readonly DeactivateCategoryHandler _deactivateCategory;
    private readonly ActivateCategoryHandler _activateCategory;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="getCategories">Query handler that loads the page of categories.</param>
    /// <param name="deactivateCategory">Command handler that soft-deletes a category.</param>
    /// <param name="activateCategory">Command handler that reactivates a category.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public IndexModel(
        GetCategoriesPagedHandler getCategories,
        DeactivateCategoryHandler deactivateCategory,
        ActivateCategoryHandler activateCategory,
        IStringLocalizer<SharedResource> localizer)
    {
        _getCategories = getCategories;
        _deactivateCategory = deactivateCategory;
        _activateCategory = activateCategory;
        _localizer = localizer;
    }

    /// <summary>Page of categories to render.</summary>
    public PagedResult<CategoryDto> Categories { get; private set; } = new([], 1, PageSize, 0);

    /// <summary>Current page number, bound from the query string.</summary>
    [BindProperty(SupportsGet = true, Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    /// <summary>Optional search text, bound from the query string.</summary>
    [BindProperty(SupportsGet = true, Name = "search")]
    public string? Search { get; set; }

    /// <summary>Handles GET requests and loads the requested page of categories.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    /// <summary>
    /// htmx endpoint that returns only the results container so the page is not reloaded. Because
    /// the search input does not send "page", a new search always restarts at page 1.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The partial view with the category rows.</returns>
    public async Task<IActionResult> OnGetRowsAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        return Partial("_CategoriesTable", this);
    }

    /// <summary>
    /// Deactivates a category. Refuses when products are still associated with it.
    /// </summary>
    /// <param name="id">Identifier of the category to deactivate.</param>
    /// <param name="pageNumber">Current page number, preserved across the redirect.</param>
    /// <param name="search">Current search text, preserved across the redirect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A redirect back to the list.</returns>
    public async Task<IActionResult> OnPostDeactivateAsync(
        Guid id,
        int pageNumber,
        string? search,
        CancellationToken cancellationToken)
    {
        var result = await _deactivateCategory.HandleAsync(new DeactivateCategoryCommand(id), cancellationToken);

        if (result.IsSuccess)
        {
            TempData["StatusMessage"] = _localizer["CategoryDeactivated"].Value;
        }
        else
        {
            TempData["ErrorMessage"] = _localizer[result.Error!].Value;
        }

        return RedirectToPage("Index", new { pageNumber, search });
    }

    /// <summary>
    /// Reactivates a category.
    /// </summary>
    /// <param name="id">Identifier of the category to activate.</param>
    /// <param name="pageNumber">Current page number, preserved across the redirect.</param>
    /// <param name="search">Current search text, preserved across the redirect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A redirect back to the list.</returns>
    public async Task<IActionResult> OnPostActivateAsync(
        Guid id,
        int pageNumber,
        string? search,
        CancellationToken cancellationToken)
    {
        var result = await _activateCategory.HandleAsync(new ActivateCategoryCommand(id), cancellationToken);

        if (result.IsSuccess)
        {
            TempData["StatusMessage"] = _localizer["CategoryActivated"].Value;
        }
        else
        {
            TempData["ErrorMessage"] = _localizer[result.Error!].Value;
        }

        return RedirectToPage("Index", new { pageNumber, search });
    }

    // Loads the page of categories.
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Categories = await _getCategories.HandleAsync(
            new GetCategoriesPagedQuery(PageNumber, PageSize, Search),
            cancellationToken);
    }
}
