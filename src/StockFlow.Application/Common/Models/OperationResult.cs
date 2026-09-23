namespace StockFlow.Application.Common.Models;

/// <summary>
/// Outcome of a use case. <see cref="Error"/> is a stable code (a localization resource key),
/// never a user-facing message, so the presentation layer can localize it.
/// </summary>
/// <typeparam name="T">Type of the value produced on success.</typeparam>
/// <param name="IsSuccess">Whether the use case completed successfully.</param>
/// <param name="Value">Result value when successful; otherwise null.</param>
/// <param name="Error">Stable error code when unsuccessful; otherwise null.</param>
public record OperationResult<T>(bool IsSuccess, T? Value, string? Error)
{
    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    /// <param name="value">Value produced by the use case.</param>
    /// <returns>A successful <see cref="OperationResult{T}"/>.</returns>
    public static OperationResult<T> Success(T value) => new(true, value, null);

    /// <summary>Creates a failed result carrying a stable error code.</summary>
    /// <param name="error">Localization resource key describing the failure.</param>
    /// <returns>A failed <see cref="OperationResult{T}"/>.</returns>
    public static OperationResult<T> Failure(string error) => new(false, default, error);
}
