using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Products;
using StockFlow.Application.Products.Commands;
using StockFlow.Domain.Entities;
using StockFlow.Domain;

namespace StockFlow.Application.Tests.Products;

/// <summary>
/// Unit tests for <see cref="DeactivateProductHandler"/>.
/// </summary>
public class DeactivateProductHandlerTests
{
    /// <summary>Deactivating an existing product marks it inactive without deleting it.</summary>
    [Fact]
    public async Task HandleAsync_WithExistingProduct_DeactivatesWithoutDeleting()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var product = Product.Create("SKU-001", "Test product", null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 5, 2, "user-1", "user@test.local", DateTimeOffset.UnixEpoch);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new DeactivateProductHandler(db);

        // Act
        var result = await handler.HandleAsync(new DeactivateProductCommand(product.Id));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsActive);
        var persisted = await db.Products.SingleAsync(p => p.Id == product.Id);
        Assert.False(persisted.IsActive);
    }

    /// <summary>Deactivating an unknown product returns the not-found error code.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new DeactivateProductHandler(db);

        // Act
        var result = await handler.HandleAsync(new DeactivateProductCommand(Guid.NewGuid()));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrorCodes.NotFound, result.Error);
    }
}
