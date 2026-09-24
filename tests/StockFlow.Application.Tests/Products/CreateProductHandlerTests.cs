using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Products;
using StockFlow.Application.Products.Commands;
using StockFlow.Domain;

namespace StockFlow.Application.Tests.Products;

/// <summary>
/// Unit tests for <see cref="CreateProductHandler"/>.
/// </summary>
public class CreateProductHandlerTests
{
    // Builds a valid command, letting each test override the SKU when needed.
    private static CreateProductCommand ValidCommand(Guid categoryId, string sku = "SKU-001") => new(
        sku,
        "Test product",
        "Description",
        categoryId,
        UnitOfMeasure.Unit,
        UnitOfMeasure.Box,
        24m,
        12m,
        1m,
        13m,
        48,
        6);

    /// <summary>Creating with valid data persists an active product.</summary>
    [Fact]
    public async Task HandleAsync_WithValidData_PersistsProduct()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var handler = new CreateProductHandler(db);

        // Act
        var result = await handler.HandleAsync(ValidCommand(categoryId));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("SKU-001", result.Value!.Sku);
        Assert.Equal(categoryId, result.Value.CategoryId);
        Assert.True(result.Value.IsActive);
        Assert.Equal(UnitOfMeasure.Unit, result.Value.BaseUnit);
        Assert.Equal(UnitOfMeasure.Box, result.Value.PurchaseUnit);
        Assert.Equal(24m, result.Value.PurchaseUnitFactor);
        Assert.Equal(0.5m, result.Value.UnitCost);
        Assert.Equal(48m, result.Value.CurrentStock);
        Assert.True(await db.Products.AnyAsync(p => p.Id == result.Value.Id));
    }

    /// <summary>Creating with a duplicate SKU fails and does not persist a second product.</summary>
    [Fact]
    public async Task HandleAsync_WithDuplicateSku_ReturnsFailure()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var handler = new CreateProductHandler(db);
        await handler.HandleAsync(ValidCommand(categoryId));

        // Act
        var result = await handler.HandleAsync(ValidCommand(categoryId));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrorCodes.SkuAlreadyExists, result.Error);
        Assert.Equal(1, await db.Products.CountAsync());
    }

    /// <summary>Creating with an unknown category fails and does not persist the product.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownCategory_ReturnsCategoryNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateProductHandler(db);

        // Act
        var result = await handler.HandleAsync(ValidCommand(Guid.NewGuid()));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrorCodes.CategoryNotFound, result.Error);
        Assert.Equal(0, await db.Products.CountAsync());
    }
}
