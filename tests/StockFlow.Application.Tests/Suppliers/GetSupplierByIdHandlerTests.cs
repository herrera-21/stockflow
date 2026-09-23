using StockFlow.Application.Suppliers.Queries;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Suppliers;

/// <summary>
/// Unit tests for <see cref="GetSupplierByIdHandler"/>.
/// </summary>
public class GetSupplierByIdHandlerTests
{
    /// <summary>Querying an existing identifier returns the projected supplier.</summary>
    [Fact]
    public async Task HandleAsync_WithExistingId_ReturnsSupplier()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var supplier = Supplier.Create("Acme Supplies S.A.", "3-101-654321", "Jane Doe", "8888-8888", "sales@acme.test", "Main street 1");
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var handler = new GetSupplierByIdHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetSupplierByIdQuery(supplier.Id));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(supplier.Id, result!.Id);
        Assert.Equal("Acme Supplies S.A.", result.Name);
        Assert.Equal("3-101-654321", result.TaxId);
        Assert.Equal("Jane Doe", result.ContactName);
    }

    /// <summary>Querying an unknown identifier returns null.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNull()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new GetSupplierByIdHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetSupplierByIdQuery(Guid.NewGuid()));

        // Assert
        Assert.Null(result);
    }
}
