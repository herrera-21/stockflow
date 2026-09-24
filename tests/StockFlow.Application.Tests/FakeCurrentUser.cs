using StockFlow.Application.Common.Interfaces;

namespace StockFlow.Application.Tests;

/// <summary>
/// Deterministic <see cref="ICurrentUser"/> for handler tests, so movements are attributed to a
/// stable user without an HTTP context.
/// </summary>
internal sealed class FakeCurrentUser : ICurrentUser
{
    /// <summary>Initializes the fake with the given identity.</summary>
    /// <param name="userId">Identifier to report; defaults to a test user.</param>
    /// <param name="userName">Display name to report; defaults to a test email.</param>
    public FakeCurrentUser(string? userId = "user-1", string? userName = "user@test.local")
    {
        UserId = userId;
        UserName = userName;
    }

    /// <summary>Identifier of the signed-in user.</summary>
    public string? UserId { get; }

    /// <summary>Display name of the signed-in user.</summary>
    public string? UserName { get; }
}
