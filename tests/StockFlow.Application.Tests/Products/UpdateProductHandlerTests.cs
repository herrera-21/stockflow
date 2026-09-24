using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Products;
using StockFlow.Application.Products.Commands;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Products;

/// <summary>
/// Unit tests for <see cref="UpdateProductHandler"/>.
/// </summary>
public class UpdateProductHandlerTests
{
    // Builds a persisted product for the update scenarios.
    private static Product NewProduct(Guid categoryId, string sku = "SKU-001", decimal initialStock = 5) =>
        Product.Create(sku, "Test product", null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, initialStock, 2, "user-1", "user@test.local", DateTimeOffset.UnixEpoch);

    /// <summary>Updating with valid data changes editable fields but keeps current stock.</summary>
    [Fact]
    public async Task HandleAsync_WithValidData_UpdatesEditableFieldsAndKeepsStock()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var otherCategoryId = db.Categories.Single(c => c.Code == "other").Id;
        var cleaningCategoryId = db.Categories.Single(c => c.Code == "cleaning").Id;
        var product = NewProduct(otherCategoryId);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new UpdateProductHandler(db);
        var command = new UpdateProductCommand(
            product.Id, "SKU-002", "Renamed", "New description", cleaningCategoryId,
            UnitOfMeasure.Unit, UnitOfMeasure.Box, 12m, 20m, 30m, 8m, 4);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        var persisted = await db.Products.SingleAsync(p => p.Id == product.Id);
        Assert.Equal("SKU-002", persisted.Sku);
        Assert.Equal("Renamed", persisted.Name);
        Assert.Equal(cleaningCategoryId, persisted.CategoryId);
        Assert.Equal(20m, persisted.PurchasePrice);
        Assert.Equal(30m, persisted.SalePrice);
        Assert.Equal(8m, persisted.TaxRate);
        Assert.Equal(UnitOfMeasure.Box, persisted.PurchaseUnit);
        Assert.Equal(12m, persisted.PurchaseUnitFactor);
        Assert.Equal(4m, persisted.MinimumStock);
        Assert.Equal(5m, persisted.CurrentStock);
    }

    /// <summary>Updating an unknown product returns the not-found error code.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var handler = new UpdateProductHandler(db);
        var command = new UpdateProductCommand(
            Guid.NewGuid(), "SKU-002", "Renamed", null, categoryId,
            UnitOfMeasure.Unit, UnitOfMeasure.Box, 12m, 20m, 30m, 8m, 4);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrorCodes.NotFound, result.Error);
    }

    /// <summary>Using the SKU of another product returns the duplicate-SKU error code.</summary>
    [Fact]
    public async Task HandleAsync_WithSkuOfAnotherProduct_ReturnsFailure()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var first = NewProduct(categoryId, "SKU-001");
        var second = NewProduct(categoryId, "SKU-002");
        db.Products.AddRange(first, second);
        await db.SaveChangesAsync();

        var handler = new UpdateProductHandler(db);
        var command = new UpdateProductCommand(
            second.Id, "SKU-001", "Renamed", null, categoryId,
            UnitOfMeasure.Unit, UnitOfMeasure.Box, 12m, 20m, 30m, 8m, 4);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrorCodes.SkuAlreadyExists, result.Error);
    }

    /// <summary>Using an unknown category returns the category-not-found error code.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownCategory_ReturnsCategoryNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var product = NewProduct(categoryId);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new UpdateProductHandler(db);
        var command = new UpdateProductCommand(
            product.Id, "SKU-002", "Renamed", null, Guid.NewGuid(),
            UnitOfMeasure.Unit, UnitOfMeasure.Box, 12m, 20m, 30m, 8m, 4);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrorCodes.CategoryNotFound, result.Error);
    }

    /// <summary>Changing the base unit while the product has stock returns the locked error code.</summary>
    [Fact]
    public async Task HandleAsync_ChangingBaseUnitWithStock_ReturnsBaseUnitLocked()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var product = NewProduct(categoryId, initialStock: 5);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new UpdateProductHandler(db);
        var command = new UpdateProductCommand(
            product.Id, "SKU-001", "Test product", null, categoryId,
            UnitOfMeasure.Pound, UnitOfMeasure.Pound, 1m, 10m, 15m, 13m, 2);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrorCodes.BaseUnitLocked, result.Error);
        var persisted = await db.Products.SingleAsync(p => p.Id == product.Id);
        Assert.Equal(UnitOfMeasure.Unit, persisted.BaseUnit);
    }

    /// <summary>Changing the base unit of a product without stock succeeds.</summary>
    [Fact]
    public async Task HandleAsync_ChangingBaseUnitWithoutStock_UpdatesUnit()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var product = NewProduct(categoryId, initialStock: 0);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new UpdateProductHandler(db);
        var command = new UpdateProductCommand(
            product.Id, "SKU-001", "Test product", null, categoryId,
            UnitOfMeasure.Pound, UnitOfMeasure.Pound, 1m, 10m, 15m, 13m, 2.5m);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(UnitOfMeasure.Pound, result.Value!.BaseUnit);
        Assert.Equal(2.5m, result.Value.MinimumStock);
    }
}
