using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Products;
using StockFlow.Application.Products.Commands;

namespace StockFlow.Application.Tests.Products;

/// <summary>
/// Unit tests for <see cref="CreateProductHandler"/>.
/// </summary>
public class CreateProductHandlerTests
{
    // Builds a valid command, letting each test override the SKU when needed.
    private static CreateProductCommand ValidCommand(string sku = "SKU-001") => new(
        sku,
        "Test product",
        "Description",
        "General",
        10m,
        15m,
        13m,
        5,
        2);

    /// <summary>Creating with valid data persists an active product.</summary>
    [Fact]
    public async Task HandleAsync_WithValidData_PersistsProduct()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateProductHandler(db);

        // Act
        var result = await handler.HandleAsync(ValidCommand());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("SKU-001", result.Value!.Sku);
        Assert.True(result.Value.IsActive);
        Assert.True(await db.Products.AnyAsync(p => p.Id == result.Value.Id));
    }

    /// <summary>Creating with a duplicate SKU fails and does not persist a second product.</summary>
    [Fact]
    public async Task HandleAsync_WithDuplicateSku_ReturnsFailure()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateProductHandler(db);
        await handler.HandleAsync(ValidCommand());

        // Act
        var result = await handler.HandleAsync(ValidCommand());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrorCodes.SkuAlreadyExists, result.Error);
        Assert.Equal(1, await db.Products.CountAsync());
    }
}
