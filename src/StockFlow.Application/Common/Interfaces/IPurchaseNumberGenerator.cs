namespace StockFlow.Application.Common.Interfaces;

/// <summary>
/// Produces the next purchase order number. The implementation reads the atomic SQL Server sequence
/// configured in Infrastructure and formats it with the domain rule
/// (<see cref="StockFlow.Domain.PurchaseNumber"/>),
/// so the format and the counter stay in their respective layers.
/// </summary>
public interface IPurchaseNumberGenerator
{
    /// <summary>
    /// Returns the next formatted purchase order number, for example <c>OC-000001</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next unique order number.</returns>
    Task<string> NextNumberAsync(CancellationToken cancellationToken = default);
}
