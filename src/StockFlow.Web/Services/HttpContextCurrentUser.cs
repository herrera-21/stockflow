using System.Security.Claims;
using StockFlow.Application.Common.Interfaces;

namespace StockFlow.Web.Services;

/// <summary>
/// Resolves the current user from the HTTP context so application handlers can attribute inventory
/// movements and audit entries without depending on ASP.NET Core.
/// </summary>
public class HttpContextCurrentUser : ICurrentUser
{
    // Provides access to the current request, including the authenticated principal.
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes the service.</summary>
    /// <param name="httpContextAccessor">Accessor for the current HTTP context.</param>
    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>Identifier of the signed-in user, or null when there is none.</summary>
    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>Display name (email) of the signed-in user, or null when there is none.</summary>
    public string? UserName =>
        _httpContextAccessor.HttpContext?.User?.Identity?.Name;
}
