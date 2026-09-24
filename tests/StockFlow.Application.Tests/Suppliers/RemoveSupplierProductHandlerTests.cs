using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Products;
using StockFlow.Application.Suppliers.Commands;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Suppliers;

/// <summary>
/// Unit tests for <see cref="RemoveSupplierProductHandler"/>.
/// </summary>
public class RemoveSupplierProductHandlerTests
{
    /// <summary>Removing an existing association deletes it without touching the product.</summary>
    [Fact]
    public async Task HandleAsync_WithExistingAssociation_RemovesIt()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var supplier = Supplier.Create("Acme Supplies S.A.", DocumentType.Dui, "00000001-1", null, null, null, null);
        var product = Product.Create("SKU-001", "Test product", null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 0, 0, "user-1", "user@test.local", DateTimeOffset.UnixEpoch);
        db.Suppliers.Add(supplier);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var assign = new AssignSupplierProductHandler(db);
        await assign.HandleAsync(new AssignSupplierProductCommand(supplier.Id, product.Id, null, null, false));

        var handler = new RemoveSupplierProductHandler(db);

        // Act
        var result = await handler.HandleAsync(new RemoveSupplierProductCommand(supplier.Id, product.Id));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(product.Id, result.Value);
        Assert.Empty(await db.ProductSuppliers.ToListAsync());
        Assert.True(await db.Products.AnyAsync(p => p.Id == product.Id));
    }

    /// <summary>An unknown product returns the product not-found error code.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownProduct_ReturnsNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new RemoveSupplierProductHandler(db);

        // Act
        var result = await handler.HandleAsync(new RemoveSupplierProductCommand(Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrorCodes.NotFound, result.Error);
    }
}
