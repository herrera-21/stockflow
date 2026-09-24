using StockFlow.Application.Products.Queries;
using StockFlow.Domain.Entities;
using StockFlow.Domain;

namespace StockFlow.Application.Tests.Products;

/// <summary>
/// Unit tests for <see cref="GetProductByIdHandler"/>.
/// </summary>
public class GetProductByIdHandlerTests
{
    /// <summary>Querying an existing identifier returns the projected product.</summary>
    [Fact]
    public async Task HandleAsync_WithExistingId_ReturnsProduct()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var product = Product.Create("SKU-001", "Test product", null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 5, 2, "user-1", "user@test.local", DateTimeOffset.UnixEpoch);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new GetProductByIdHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetProductByIdQuery(product.Id));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product.Id, result!.Id);
        Assert.Equal("SKU-001", result.Sku);
    }

    /// <summary>Querying an unknown identifier returns null.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNull()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new GetProductByIdHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetProductByIdQuery(Guid.NewGuid()));

        // Assert
        Assert.Null(result);
    }
}
