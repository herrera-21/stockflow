using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StockFlow.Web.Pages.Account;

/// <summary>
/// Access denied page shown when an authenticated user lacks the required policy.
/// </summary>
public class AccessDeniedModel : PageModel
{
    /// <summary>Handles GET requests for the access denied page.</summary>
    public void OnGet()
    {
    }
}
