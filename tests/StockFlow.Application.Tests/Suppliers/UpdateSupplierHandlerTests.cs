using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Suppliers;
using StockFlow.Application.Suppliers.Commands;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Suppliers;

/// <summary>
/// Unit tests for <see cref="UpdateSupplierHandler"/>.
/// </summary>
public class UpdateSupplierHandlerTests
{
    // Builds a persisted supplier for the update scenarios.
    private static Supplier NewSupplier(string taxId = "3-101-654321") =>
        Supplier.Create("Acme Supplies S.A.", taxId, null, null, null, null);

    /// <summary>Updating with valid data changes the editable fields.</summary>
    [Fact]
    public async Task HandleAsync_WithValidData_UpdatesEditableFields()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var supplier = NewSupplier();
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var handler = new UpdateSupplierHandler(db);
        var command = new UpdateSupplierCommand(
            supplier.Id, "Renamed Supplies S.A.", "3-101-999999", "John Roe", "7777-7777", "new@acme.test", "New address 5");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        var persisted = await db.Suppliers.SingleAsync(s => s.Id == supplier.Id);
        Assert.Equal("Renamed Supplies S.A.", persisted.Name);
        Assert.Equal("3-101-999999", persisted.TaxId);
        Assert.Equal("John Roe", persisted.ContactName);
        Assert.Equal("7777-7777", persisted.Phone);
        Assert.Equal("new@acme.test", persisted.Email);
        Assert.Equal("New address 5", persisted.Address);
    }

    /// <summary>Updating an unknown supplier returns the not-found error code.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateSupplierHandler(db);
        var command = new UpdateSupplierCommand(
            Guid.NewGuid(), "Renamed Supplies S.A.", "3-101-999999", null, null, null, null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SupplierErrorCodes.NotFound, result.Error);
    }

    /// <summary>Using the tax id of another supplier returns the duplicate error code.</summary>
    [Fact]
    public async Task HandleAsync_WithTaxIdOfAnotherSupplier_ReturnsFailure()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var first = NewSupplier("3-101-654321");
        var second = NewSupplier("3-101-999999");
        db.Suppliers.AddRange(first, second);
        await db.SaveChangesAsync();

        var handler = new UpdateSupplierHandler(db);
        var command = new UpdateSupplierCommand(
            second.Id, "Renamed Supplies S.A.", "3-101-654321", null, null, null, null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SupplierErrorCodes.TaxIdAlreadyExists, result.Error);
    }
}
