using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StockFlow.Web.Pages.Account;

/// <summary>
/// Login page. Authenticates a user with ASP.NET Core Identity and redirects to the requested page.
/// </summary>
public class LoginModel : PageModel
{
    // Identity sign-in service used to validate credentials.
    private readonly SignInManager<IdentityUser> _signInManager;

    /// <summary>Initializes the page model.</summary>
    /// <param name="signInManager">Identity sign-in service.</param>
    public LoginModel(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    /// <summary>Credentials posted by the user.</summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>True when the last sign-in attempt failed.</summary>
    public bool LoginFailed { get; set; }

    /// <summary>URL to return to after a successful sign-in.</summary>
    public string ReturnUrl { get; set; } = "/";

    /// <summary>Handles GET requests and captures the return URL.</summary>
    /// <param name="returnUrl">Requested URL to return to after sign-in, when provided.</param>
    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    /// <summary>
    /// Validates the posted credentials and signs the user in.
    /// </summary>
    /// <param name="returnUrl">Requested URL to return to after sign-in, when provided.</param>
    /// <returns>A redirect on success, or the page with an error when the credentials are invalid.</returns>
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return LocalRedirect(ReturnUrl);
        }

        LoginFailed = true;
        return Page();
    }

    /// <summary>
    /// Credentials captured by the login form.
    /// </summary>
    public class InputModel
    {
        /// <summary>User email address.</summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>User password.</summary>
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        /// <summary>Whether the session should persist across browser restarts.</summary>
        public bool RememberMe { get; set; }
    }
}
