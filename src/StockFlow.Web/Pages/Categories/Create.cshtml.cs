using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using StockFlow.Application.Categories.Commands;
using StockFlow.Web.Authorization;

namespace StockFlow.Web.Pages.Categories;

/// <summary>
/// Create category page. Only administrators can access it.
/// </summary>
[Authorize(Policy = Policies.CanManageCategories)]
public class CreateModel : PageModel
{
    // Use case and localizer used to create the category and report errors.
    private readonly CreateCategoryHandler _createCategory;
    private readonly IStringLocalizer<SharedResource> _localizer;

    /// <summary>Initializes the page model.</summary>
    /// <param name="createCategory">Command handler that creates the category.</param>
    /// <param name="localizer">Localizer for UI messages.</param>
    public CreateModel(CreateCategoryHandler createCategory, IStringLocalizer<SharedResource> localizer)
    {
        _createCategory = createCategory;
        _localizer = localizer;
    }

    /// <summary>Data posted by the create form.</summary>
    [BindProperty]
    public CreateCategoryInputModel Input { get; set; } = new();

    /// <summary>Handles GET requests.</summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Validates the form and creates the category.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A redirect to the list on success, or the page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _createCategory.HandleAsync(new CreateCategoryCommand(Input.Name), cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("Input.Name", _localizer[result.Error!]);
            return Page();
        }

        TempData["StatusMessage"] = _localizer["CategoryCreated"].Value;
        return RedirectToPage("Index");
    }
}
