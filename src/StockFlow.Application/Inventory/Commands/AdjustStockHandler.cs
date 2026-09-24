using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Application.Common.Models;
using StockFlow.Domain;

namespace StockFlow.Application.Inventory.Commands;

/// <summary>
/// Handles <see cref="AdjustStockCommand"/>: validates the adjustment against the product rules and
/// records stock plus movement in a single atomic save. The movement and the stock change cannot
/// diverge because they are committed together.
/// </summary>
public class AdjustStockHandler
{
    // Fallback user for callers without an authenticated principal (should not happen in the app).
    private const string SystemUser = "system";

    // Persistence abstraction used to load and save the product and its movement.
    private readonly IAppDbContext _db;

    // Current user that the movement is attributed to.
    private readonly ICurrentUser _currentUser;

    // Clock used to stamp the movement.
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes the handler.</summary>
    /// <param name="db">Persistence abstraction.</param>
    /// <param name="currentUser">Current user that produces the adjustment.</param>
    /// <param name="timeProvider">Clock used to stamp the movement.</param>
    public AdjustStockHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Applies a manual adjustment unless the product is missing, inactive, the values are invalid or
    /// the resulting stock would be negative.
    /// </summary>
    /// <param name="command">Adjustment data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success carrying the recorded movement, or a failure with a stable error code.</returns>
    public async Task<OperationResult<InventoryMovementDto>> HandleAsync(
        AdjustStockCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);

        if (product is null)
        {
            return OperationResult<InventoryMovementDto>.Failure(InventoryErrorCodes.ProductNotFound);
        }

        if (!product.IsActive)
        {
            return OperationResult<InventoryMovementDto>.Failure(InventoryErrorCodes.ProductInactive);
        }

        if (!Enum.IsDefined(command.Type) || !command.Type.IsAdjustment())
        {
            return OperationResult<InventoryMovementDto>.Failure(InventoryErrorCodes.InvalidMovement);
        }

        if (command.Quantity <= 0 || !product.BaseUnit.IsValidQuantity(command.Quantity))
        {
            return OperationResult<InventoryMovementDto>.Failure(InventoryErrorCodes.InvalidQuantity);
        }

        if (command.Reason is null)
        {
            return OperationResult<InventoryMovementDto>.Failure(InventoryErrorCodes.AdjustmentReasonRequired);
        }

        if (command.Reason == InventoryAdjustmentReason.Other && string.IsNullOrWhiteSpace(command.Note))
        {
            return OperationResult<InventoryMovementDto>.Failure(InventoryErrorCodes.AdjustmentNoteRequired);
        }

        if (product.GetResultingStock(command.Type, command.Quantity) < 0)
        {
            return OperationResult<InventoryMovementDto>.Failure(InventoryErrorCodes.InsufficientStock);
        }

        var movement = product.ApplyMovement(
            command.Type,
            command.Quantity,
            command.Reason,
            command.Note,
            _currentUser.UserId ?? SystemUser,
            _currentUser.UserName ?? SystemUser,
            _timeProvider.GetUtcNow());

        // The movement is added explicitly so EF Core tracks it as new even though its identifier is
        // already assigned by the domain.
        _db.InventoryMovements.Add(movement);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another operation changed the stock between the read and the save.
            return OperationResult<InventoryMovementDto>.Failure(InventoryErrorCodes.ConcurrencyConflict);
        }

        return OperationResult<InventoryMovementDto>.Success(movement.ToDto());
    }
}
