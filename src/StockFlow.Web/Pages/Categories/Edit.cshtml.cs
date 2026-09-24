using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Categories;
using StockFlow.Application.Categories.Commands;
using StockFlow.Application.Categories.Queries;
using StockFlow.Web.Authorization;

namespace StockFlow.Web.Pages.Categories;

/// <summary>
/// Edit category page. Only administrators can access it.
/// </summary>
[Authorize(Policy = Policies.CanManageCategories)]
public class EditModel : PageModel
{
    // Use cases and localizer used to load and update the category.
    private readonly GetCategoryByIdHandler _getCategory;
    private readonly UpdateCategoryHandler _updateCategory;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="getCategory">Query handler that loads the category.</param>
    /// <param name="updateCategory">Command handler that updates the category.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public EditModel(
        GetCategoryByIdHandler getCategory,
        UpdateCategoryHandler updateCategory,
        IStringLocalizer<SharedResource> localizer)
    {
        _getCategory = getCategory;
        _updateCategory = updateCategory;
        _localizer = localizer;
    }

    /// <summary>Data posted by the edit form.</summary>
    [BindProperty]
    public UpdateCategoryInputModel Input { get; set; } = new();

    /// <summary>
    /// Loads the category into the form.
    /// </summary>
    /// <param name="id">Identifier of the category to edit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page, or a not-found response when the category does not exist.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await _getCategory.HandleAsync(new GetCategoryByIdQuery(id), cancellationToken);

        if (category is null)
        {
            return NotFound();
        }

        Input = new UpdateCategoryInputModel
        {
            Id = category.Id,
            Name = category.Name
        };

        return Page();
    }

    /// <summary>
    /// Validates the form and updates the category.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A redirect to the list on success, or the page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _updateCategory.HandleAsync(
            new UpdateCategoryCommand(Input.Id, Input.Name),
            cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == CategoryErrorCodes.NotFound)
            {
                return NotFound();
            }

            ModelState.AddModelError("Input.Name", _localizer[result.Error!]);
            return Page();
        }

        TempData["StatusMessage"] = _localizer["CategoryUpdated"].Value;
        return RedirectToPage("Index");
    }
}
