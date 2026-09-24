using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Products;
using StockFlow.Application.Suppliers;
using StockFlow.Application.Suppliers.Commands;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Suppliers;

/// <summary>
/// Unit tests for <see cref="AssignSupplierProductHandler"/>: creating, updating and the
/// single-preferred-supplier rule.
/// </summary>
public class AssignSupplierProductHandlerTests
{
    // Builds a valid product, letting each test override the SKU when needed.
    private static Product NewProduct(Guid categoryId, string sku = "SKU-001") =>
        Product.Create(sku, "Test product", null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 0, 0, "user-1", "user@test.local", DateTimeOffset.UnixEpoch);

    // Builds a valid supplier.
    private static Supplier NewSupplier(string taxId = "00000001-1") =>
        Supplier.Create("Acme Supplies S.A.", DocumentType.Dui, taxId, null, null, null, null);

    /// <summary>Associating a product with a supplier persists the association with its data.</summary>
    [Fact]
    public async Task HandleAsync_WithValidData_CreatesAssociation()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var supplier = NewSupplier();
        var product = NewProduct(categoryId);
        db.Suppliers.Add(supplier);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new AssignSupplierProductHandler(db);
        var command = new AssignSupplierProductCommand(supplier.Id, product.Id, "SUP-1", 8.5m, IsPreferred: true);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Test product", result.Value!.ProductName);
        Assert.Equal("SUP-1", result.Value.SupplierSku);
        Assert.Equal(8.5m, result.Value.PurchasePrice);
        Assert.True(result.Value.IsPreferred);

        var persisted = await db.ProductSuppliers.SingleAsync(ps => ps.SupplierId == supplier.Id && ps.ProductId == product.Id);
        Assert.Equal("SUP-1", persisted.SupplierSku);
    }

    /// <summary>Associating the same product and supplier again updates the existing association.</summary>
    [Fact]
    public async Task HandleAsync_WhenAssociationExists_UpdatesIt()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var supplier = NewSupplier();
        var product = NewProduct(categoryId);
        db.Suppliers.Add(supplier);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new AssignSupplierProductHandler(db);
        await handler.HandleAsync(new AssignSupplierProductCommand(supplier.Id, product.Id, "SUP-1", 8.5m, false));

        // Act
        var result = await handler.HandleAsync(new AssignSupplierProductCommand(supplier.Id, product.Id, "SUP-2", 9.0m, false));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, await db.ProductSuppliers.CountAsync());
        Assert.Equal("SUP-2", result.Value!.SupplierSku);
        Assert.Equal(9.0m, result.Value.PurchasePrice);
    }

    /// <summary>Marking a supplier as preferred clears the flag from the previously preferred one.</summary>
    [Fact]
    public async Task HandleAsync_WhenPreferred_ClearsPreviousPreferred()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var first = NewSupplier("00000001-1");
        var second = NewSupplier("00000002-2");
        var product = NewProduct(categoryId);
        db.Suppliers.AddRange(first, second);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new AssignSupplierProductHandler(db);
        await handler.HandleAsync(new AssignSupplierProductCommand(first.Id, product.Id, null, null, IsPreferred: true));

        // Act
        await handler.HandleAsync(new AssignSupplierProductCommand(second.Id, product.Id, null, null, IsPreferred: true));

        // Assert
        var preferred = await db.ProductSuppliers.Where(ps => ps.ProductId == product.Id && ps.IsPreferred).ToListAsync();
        var association = Assert.Single(preferred);
        Assert.Equal(second.Id, association.SupplierId);
    }

    /// <summary>An unknown supplier returns the supplier not-found error code.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownSupplier_ReturnsNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var product = NewProduct(categoryId);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new AssignSupplierProductHandler(db);

        // Act
        var result = await handler.HandleAsync(new AssignSupplierProductCommand(Guid.NewGuid(), product.Id, null, null, false));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SupplierErrorCodes.NotFound, result.Error);
    }

    /// <summary>An unknown product returns the product not-found error code.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownProduct_ReturnsNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var supplier = NewSupplier();
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var handler = new AssignSupplierProductHandler(db);

        // Act
        var result = await handler.HandleAsync(new AssignSupplierProductCommand(supplier.Id, Guid.NewGuid(), null, null, false));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProductErrorCodes.NotFound, result.Error);
    }
}
