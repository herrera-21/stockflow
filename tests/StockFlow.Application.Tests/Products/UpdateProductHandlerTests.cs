using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Products;
using StockFlow.Application.Products.Commands;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Products;

/// <summary>
/// Unit tests for <see cref="UpdateProductHandler"/>.
/// </summary>
public class UpdateProductHandlerTests
{
    // Builds a persisted product for the update scenarios.
    private static Product NewProduct(string sku = "SKU-001", int initialStock = 5) =>
        Product.Create(sku, "Test product", null, "General", 10m, 15m, 13m, initialStock, 2);

    /// <summary>Updating with valid data changes editable fields but keeps current stock.</summary>
    [Fact]
    public async Task HandleAsync_WithValidData_UpdatesEditableFieldsAndKeepsStock()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var product = NewProduct();
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new UpdateProductHandler(db);
        var command = new UpdateProductCommand(
            product.Id, "SKU-002", "Renamed", "New description", "Hardware", 20m, 30m, 8m, 4);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        var persisted = await db.Products.SingleAsync(p => p.Id == product.Id);
        Assert.Equal("SKU-002", persisted.Sku);
        Assert.Equal("Renamed", persisted.Name);
        Assert.Equal("Hardware", persisted.Category);
        Assert.Equal(20m, persisted.PurchasePrice);
        Assert.Equal(30m, persisted.SalePrice);
        Assert.Equal(8m, persisted.TaxRate);
        Assert.Equal(4, persisted.MinimumStock);
        Assert.Equal(5, persisted.CurrentStock);
    }

    /// <summary>Updating an unknown product returns the not-found error code.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateProductHandler(db);
        var command = new UpdateProductCommand(
            Guid.NewGuid(), "SKU-002", "Renamed", null, "Hardware", 20m, 30m, 8m, 4);

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
        var first = NewProduct("SKU-001");
        var second = NewProduct("SKU-002");
        db.Products.AddRange(first, second);
        await db.SaveChangesAsync();

        var handler = new UpdateProductHandler(db);
        var command = new UpdateProductCommand(
            second.Id, "SKU-001", "Renamed", null, "Hardware", 20m, 30m, 8m, 4);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrorCodes.SkuAlreadyExists, result.Error);
    }
}
