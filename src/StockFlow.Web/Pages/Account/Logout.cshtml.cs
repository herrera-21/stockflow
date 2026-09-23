using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StockFlow.Web.Pages.Account;

/// <summary>
/// Logout page. Signs the current user out and redirects to the given URL.
/// </summary>
public class LogoutModel : PageModel
{
    // Identity sign-in service used to sign the user out.
    private readonly SignInManager<IdentityUser> _signInManager;

    /// <summary>Initializes the page model.</summary>
    /// <param name="signInManager">Identity sign-in service.</param>
    public LogoutModel(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    /// <summary>
    /// Signs the user out.
    /// </summary>
    /// <param name="returnUrl">URL to redirect to; defaults to the home page.</param>
    /// <returns>A redirect to <paramref name="returnUrl"/>.</returns>
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        return LocalRedirect(returnUrl ?? Url.Content("~/"));
    }
}
