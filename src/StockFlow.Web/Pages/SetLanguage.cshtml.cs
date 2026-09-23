using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StockFlow.Web.Pages;

/// <summary>
/// Persists the language chosen in the selector by writing the request-localization cookie.
/// </summary>
public class SetLanguageModel : PageModel
{
    /// <summary>
    /// Stores the selected culture in a one-year cookie and redirects back to the originating page.
    /// </summary>
    /// <param name="culture">Culture name to switch to (for example "es" or "en").</param>
    /// <param name="returnUrl">Local URL to redirect back to.</param>
    /// <returns>A redirect to <paramref name="returnUrl"/>.</returns>
    public IActionResult OnPost(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

        return LocalRedirect(returnUrl);
    }
}
