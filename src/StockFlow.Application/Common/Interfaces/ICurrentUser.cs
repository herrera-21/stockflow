namespace StockFlow.Application.Common.Interfaces;

/// <summary>
/// Current authenticated user, used to attribute inventory movements and audit entries.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Identifier of the signed-in user, or null when there is none.</summary>
    string? UserId { get; }

    /// <summary>Display name (email) of the signed-in user, or null when there is none.</summary>
    string? UserName { get; }
}
