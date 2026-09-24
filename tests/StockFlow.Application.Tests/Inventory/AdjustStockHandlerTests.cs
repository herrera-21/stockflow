using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using StockFlow.Application.Inventory;
using StockFlow.Application.Inventory.Commands;
using StockFlow.Domain;
using StockFlow.Domain.Entities;
using StockFlow.Infrastructure.Persistence;

namespace StockFlow.Application.Tests.Inventory;

/// <summary>
/// Unit tests for <see cref="AdjustStockHandler"/>: the valid adjustment updates stock and records
/// one movement, and every invalid adjustment leaves the stock untouched.
/// </summary>
public class AdjustStockHandlerTests
{
    // Builds a valid adjustment command, letting each test override what it needs.
    private static AdjustStockCommand Command(
        Guid productId,
        InventoryMovementType type = InventoryMovementType.AdjustmentIncrease,
        decimal quantity = 5m,
        InventoryAdjustmentReason? reason = InventoryAdjustmentReason.PhysicalCount,
        string? note = null) =>
        new(productId, type, quantity, reason, note);

    // Seeds an active product with the given stock and returns it.
    private static Product SeedProduct(AppDbContext db, decimal stock = 10m, UnitOfMeasure unit = UnitOfMeasure.Unit, bool active = true)
    {
        var categoryId = db.Categories.First().Id;
        var product = Product.Create(
            "SKU-001", "Test product", null, categoryId, unit, unit, 1m, 10m, 15m, 13m, stock, 0,
            "seed", "seed@test.local", DateTimeOffset.UnixEpoch);

        if (!active)
        {
            product.Deactivate();
        }

        db.Products.Add(product);
        db.SaveChanges();
        return product;
    }

    /// <summary>A valid positive adjustment updates the stock and records the movement.</summary>
    [Fact]
    public async Task HandleAsync_WithValidIncrease_UpdatesStockAndRecordsMovement()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var product = SeedProduct(db, stock: 10m);
        var handler = new AdjustStockHandler(db, new FakeCurrentUser(), new FakeTimeProvider());

        // Act
        var result = await handler.HandleAsync(Command(product.Id, quantity: 5m));

        // Assert
        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(InventoryMovementType.AdjustmentIncrease, result.Value!.Type);
        Assert.Equal(10m, result.Value.StockBefore);
        Assert.Equal(15m, result.Value.StockAfter);
        Assert.Equal("user@test.local", result.Value.UserName);

        var saved = await db.Products.SingleAsync(p => p.Id == product.Id);
        Assert.Equal(15m, saved.CurrentStock);

        var movement = await db.InventoryMovements
            .SingleAsync(m => m.ProductId == product.Id && m.Type == InventoryMovementType.AdjustmentIncrease);
        Assert.Equal(5m, movement.Quantity);
        Assert.Equal(InventoryAdjustmentReason.PhysicalCount, movement.Reason);
    }

    /// <summary>A valid negative adjustment decreases the stock and records the movement.</summary>
    [Fact]
    public async Task HandleAsync_WithValidDecrease_UpdatesStockAndRecordsMovement()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var product = SeedProduct(db, stock: 10m);
        var handler = new AdjustStockHandler(db, new FakeCurrentUser(), new FakeTimeProvider());

        // Act
        var result = await handler.HandleAsync(Command(
            product.Id, InventoryMovementType.AdjustmentDecrease, 4m, InventoryAdjustmentReason.Damage));

        // Assert
        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(10m, result.Value!.StockBefore);
        Assert.Equal(6m, result.Value.StockAfter);

        var saved = await db.Products.SingleAsync(p => p.Id == product.Id);
        Assert.Equal(6m, saved.CurrentStock);
    }

    /// <summary>A decrease that would leave the stock negative is rejected and changes nothing.</summary>
    [Fact]
    public async Task HandleAsync_WithInsufficientStock_ReturnsFailureAndKeepsStock()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var product = SeedProduct(db, stock: 3m);
        var handler = new AdjustStockHandler(db, new FakeCurrentUser(), new FakeTimeProvider());

        // Act
        var result = await handler.HandleAsync(Command(
            product.Id, InventoryMovementType.AdjustmentDecrease, 5m, InventoryAdjustmentReason.Damage));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InventoryErrorCodes.InsufficientStock, result.Error);
        Assert.Equal(3m, (await db.Products.SingleAsync(p => p.Id == product.Id)).CurrentStock);
        Assert.False(await db.InventoryMovements.AnyAsync(m => m.Type == InventoryMovementType.AdjustmentDecrease));
    }

    /// <summary>A zero quantity is rejected.</summary>
    [Fact]
    public async Task HandleAsync_WithZeroQuantity_ReturnsInvalidQuantity()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var product = SeedProduct(db);
        var handler = new AdjustStockHandler(db, new FakeCurrentUser(), new FakeTimeProvider());

        // Act
        var result = await handler.HandleAsync(Command(product.Id, quantity: 0m));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InventoryErrorCodes.InvalidQuantity, result.Error);
        Assert.False(await db.InventoryMovements.AnyAsync(m => m.Type == InventoryMovementType.AdjustmentIncrease));
    }

    /// <summary>A fractional quantity for a countable unit is rejected.</summary>
    [Fact]
    public async Task HandleAsync_WithFractionalQuantityForCountableUnit_ReturnsInvalidQuantity()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var product = SeedProduct(db, unit: UnitOfMeasure.Unit);
        var handler = new AdjustStockHandler(db, new FakeCurrentUser(), new FakeTimeProvider());

        // Act
        var result = await handler.HandleAsync(Command(product.Id, quantity: 2.5m));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InventoryErrorCodes.InvalidQuantity, result.Error);
    }

    /// <summary>An adjustment without a reason is rejected.</summary>
    [Fact]
    public async Task HandleAsync_WithoutReason_ReturnsReasonRequired()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var product = SeedProduct(db);
        var handler = new AdjustStockHandler(db, new FakeCurrentUser(), new FakeTimeProvider());

        // Act
        var result = await handler.HandleAsync(Command(product.Id, reason: null));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InventoryErrorCodes.AdjustmentReasonRequired, result.Error);
    }

    /// <summary>An adjustment with reason "other" and no note is rejected.</summary>
    [Fact]
    public async Task HandleAsync_WithOtherReasonAndNoNote_ReturnsNoteRequired()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var product = SeedProduct(db);
        var handler = new AdjustStockHandler(db, new FakeCurrentUser(), new FakeTimeProvider());

        // Act
        var result = await handler.HandleAsync(Command(
            product.Id, reason: InventoryAdjustmentReason.Other, note: null));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InventoryErrorCodes.AdjustmentNoteRequired, result.Error);
    }

    /// <summary>Adjusting an inactive product is rejected.</summary>
    [Fact]
    public async Task HandleAsync_WithInactiveProduct_ReturnsProductInactive()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var product = SeedProduct(db, active: false);
        var handler = new AdjustStockHandler(db, new FakeCurrentUser(), new FakeTimeProvider());

        // Act
        var result = await handler.HandleAsync(Command(product.Id));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InventoryErrorCodes.ProductInactive, result.Error);
    }

    /// <summary>Adjusting a missing product is rejected.</summary>
    [Fact]
    public async Task HandleAsync_WithMissingProduct_ReturnsProductNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new AdjustStockHandler(db, new FakeCurrentUser(), new FakeTimeProvider());

        // Act
        var result = await handler.HandleAsync(Command(Guid.NewGuid()));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InventoryErrorCodes.ProductNotFound, result.Error);
    }

    /// <summary>A system type that is not an adjustment is rejected.</summary>
    [Fact]
    public async Task HandleAsync_WithNonAdjustmentType_ReturnsInvalidMovement()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var product = SeedProduct(db);
        var handler = new AdjustStockHandler(db, new FakeCurrentUser(), new FakeTimeProvider());

        // Act
        var result = await handler.HandleAsync(Command(
            product.Id, InventoryMovementType.PurchaseIn, 5m, reason: null));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InventoryErrorCodes.InvalidMovement, result.Error);
    }
}
