using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Suppliers;
using StockFlow.Application.Suppliers.Commands;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Suppliers;

/// <summary>
/// Unit tests for <see cref="DeactivateSupplierHandler"/>.
/// </summary>
public class DeactivateSupplierHandlerTests
{
    /// <summary>Deactivating an existing supplier marks it inactive without deleting it.</summary>
    [Fact]
    public async Task HandleAsync_WithExistingSupplier_DeactivatesWithoutDeleting()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var supplier = Supplier.Create("Acme Supplies S.A.", "3-101-654321", null, null, null, null);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var handler = new DeactivateSupplierHandler(db);

        // Act
        var result = await handler.HandleAsync(new DeactivateSupplierCommand(supplier.Id));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsActive);
        var persisted = await db.Suppliers.SingleAsync(s => s.Id == supplier.Id);
        Assert.False(persisted.IsActive);
    }

    /// <summary>Deactivating an unknown supplier returns the not-found error code.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new DeactivateSupplierHandler(db);

        // Act
        var result = await handler.HandleAsync(new DeactivateSupplierCommand(Guid.NewGuid()));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SupplierErrorCodes.NotFound, result.Error);
    }
}
