using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StockFlow.Web.Pages;

/// <summary>
/// Home route. Redirects to the product list, which is the application's landing page.
/// </summary>
public class IndexModel : PageModel
{
    /// <summary>Redirects to the product list.</summary>
    /// <returns>A redirect to the products page.</returns>
    public IActionResult OnGet() => RedirectToPage("/Products/Index");
}
