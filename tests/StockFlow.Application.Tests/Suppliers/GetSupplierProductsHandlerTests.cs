using StockFlow.Application.Suppliers.Commands;
using StockFlow.Application.Suppliers.Queries;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Suppliers;

/// <summary>
/// Unit tests for <see cref="GetSupplierProductsHandler"/>: filtering by supplier and ordering by
/// product name.
/// </summary>
public class GetSupplierProductsHandlerTests
{
    // Builds a valid product with the given name and SKU.
    private static Product NewProduct(string name, string sku) =>
        Product.Create(sku, name, null, "General", 10m, 15m, 13m, 0, 0);

    // Builds a valid supplier.
    private static Supplier NewSupplier(string taxId) =>
        Supplier.Create("Supplier", taxId, null, null, null, null);

    /// <summary>Only the associations of the requested supplier are returned, ordered by product name.</summary>
    [Fact]
    public async Task HandleAsync_ReturnsProductsOfSupplierOrderedByName()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var supplier = NewSupplier("3-101-000001");
        var otherSupplier = NewSupplier("3-101-000002");
        var first = NewProduct("B product", "SKU-B");
        var second = NewProduct("A product", "SKU-A");
        var other = NewProduct("Other product", "SKU-O");
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
}
