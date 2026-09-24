using StockFlow.Application.Suppliers.Commands;
using StockFlow.Application.Suppliers.Queries;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Suppliers;

/// <summary>
/// Unit tests for <see cref="GetSupplierProductsHandler"/>: filtering by supplier and ordering by
/// product name.
/// </summary>
public class GetSupplierProductsHandlerTests
{
    // Builds a valid product with the given name and SKU.
    private static Product NewProduct(Guid categoryId, string name, string sku) =>
        Product.Create(sku, name, null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 0, 0);

    // Builds a valid supplier.
    private static Supplier NewSupplier(string taxId) =>
        Supplier.Create("Supplier", DocumentType.Dui, taxId, null, null, null, null);

    /// <summary>Only the associations of the requested supplier are returned, ordered by product name.</summary>
    [Fact]
    public async Task HandleAsync_ReturnsProductsOfSupplierOrderedByName()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var supplier = NewSupplier("00000001-1");
        var otherSupplier = NewSupplier("00000002-2");
        var first = NewProduct(categoryId, "B product", "SKU-B");
        var second = NewProduct(categoryId, "A product", "SKU-A");
        var other = NewProduct(categoryId, "Other product", "SKU-O");
        db.Suppliers.AddRange(supplier, otherSupplier);
        db.Products.AddRange(first, second, other);
        await db.SaveChangesAsync();

        var assign = new AssignSupplierProductHandler(db);
        await assign.HandleAsync(new AssignSupplierProductCommand(supplier.Id, first.Id, "SUP-B", 1m, false));
        await assign.HandleAsync(new AssignSupplierProductCommand(supplier.Id, second.Id, "SUP-A", 2m, true));
        await assign.HandleAsync(new AssignSupplierProductCommand(otherSupplier.Id, other.Id, null, null, false));

        var handler = new GetSupplierProductsHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetSupplierProductsQuery(supplier.Id));

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("A product", result[0].ProductName);
        Assert.Equal("SUP-A", result[0].SupplierSku);
        Assert.True(result[0].IsPreferred);
        Assert.Equal("B product", result[1].ProductName);
    }

    /// <summary>A supplier with no associations returns an empty list.</summary>
    [Fact]
    public async Task HandleAsync_WithNoAssociations_ReturnsEmpty()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new GetSupplierProductsHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetSupplierProductsQuery(Guid.NewGuid()));

        // Assert
        Assert.Empty(result);
    }

    /// <summary>The association carries the product's purchase unit, which the agreed price refers to.</summary>
    [Fact]
    public async Task HandleAsync_ReturnsPurchaseUnitOfProduct()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        var supplier = NewSupplier("00000003-3");
        var product = Product.Create(
            "SKU-BOX", "Boxed product", null, categoryId,
            UnitOfMeasure.Unit, UnitOfMeasure.Box, 24m, 20m, 2m, 13m, 0, 0);
        db.Suppliers.Add(supplier);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var assign = new AssignSupplierProductHandler(db);
        var assigned = await assign.HandleAsync(
            new AssignSupplierProductCommand(supplier.Id, product.Id, "FAB-750", 19.5m, true));

        var handler = new GetSupplierProductsHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetSupplierProductsQuery(supplier.Id));

        // Assert
        Assert.Equal(UnitOfMeasure.Box, assigned.Value!.PurchaseUnit);
        var association = Assert.Single(result);
        Assert.Equal(UnitOfMeasure.Box, association.PurchaseUnit);
        Assert.Equal(19.5m, association.PurchasePrice);
    }
}
