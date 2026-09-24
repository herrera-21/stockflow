using Microsoft.Extensions.Time.Testing;
using StockFlow.Application.Inventory.Queries;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Inventory;

/// <summary>
/// Unit tests for <see cref="GetMovementsPagedHandler"/>: movements are returned newest first and
/// only for the requested product.
/// </summary>
public class GetMovementsPagedHandlerTests
{
    /// <summary>Only the movements of the requested product are returned.</summary>
    [Fact]
    public async Task HandleAsync_ReturnsOnlyRequestedProductMovements()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var clock = new FakeTimeProvider();

        var target = Product.Create(
            "SKU-001", "Target", null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 10, 0,
            "user-1", "user@test.local", clock.GetUtcNow());
        var other = Product.Create(
            "SKU-002", "Other", null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 10, 0,
            "user-1", "user@test.local", clock.GetUtcNow());

        clock.Advance(TimeSpan.FromMinutes(1));
        target.ApplyMovement(InventoryMovementType.AdjustmentIncrease, 5m, InventoryAdjustmentReason.PhysicalCount, null, "user-1", "user@test.local", clock.GetUtcNow());
        other.ApplyMovement(InventoryMovementType.AdjustmentIncrease, 3m, InventoryAdjustmentReason.PhysicalCount, null, "user-1", "user@test.local", clock.GetUtcNow());

        db.Products.AddRange(target, other);
        await db.SaveChangesAsync();

        var handler = new GetMovementsPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetMovementsPagedQuery(target.Id, 1, 10));

        // Assert: the opening balance plus the manual adjustment, and nothing from the other product.
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(target.Id, item.ProductId));
        Assert.Equal(InventoryMovementType.AdjustmentIncrease, result.Items[0].Type);
        Assert.Equal(InventoryMovementType.InitialBalance, result.Items[1].Type);
    }

    /// <summary>Paging returns the requested slice with the correct metadata.</summary>
    [Fact]
    public async Task HandleAsync_WithPaging_ReturnsRequestedSlice()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var clock = new FakeTimeProvider();
        var product = Product.Create(
            "SKU-001", "Product", null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 0, 0,
            "user-1", "user@test.local", clock.GetUtcNow());

        for (var i = 0; i < 5; i++)
        {
            clock.Advance(TimeSpan.FromMinutes(1));
            product.ApplyMovement(InventoryMovementType.AdjustmentIncrease, 1m, InventoryAdjustmentReason.PhysicalCount, null, "user-1", "user@test.local", clock.GetUtcNow());
        }

        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new GetMovementsPagedHandler(db);

        // Act
        var firstPage = await handler.HandleAsync(new GetMovementsPagedQuery(product.Id, 1, 2));
        var thirdPage = await handler.HandleAsync(new GetMovementsPagedQuery(product.Id, 3, 2));

        // Assert
        Assert.Equal(5, firstPage.TotalCount);
        Assert.Equal(3, firstPage.TotalPages);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.True(firstPage.HasNextPage);
        Assert.False(firstPage.HasPreviousPage);

        Assert.Single(thirdPage.Items);
        Assert.True(thirdPage.HasPreviousPage);
        Assert.False(thirdPage.HasNextPage);
    }
}
