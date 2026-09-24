using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Suppliers;
using StockFlow.Application.Suppliers.Commands;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Suppliers;

/// <summary>
/// Unit tests for <see cref="UpdateSupplierHandler"/>.
/// </summary>
public class UpdateSupplierHandlerTests
{
    // Builds a persisted supplier for the update scenarios.
    private static Supplier NewSupplier(DocumentType documentType = DocumentType.Nit, string taxId = "0614-010101-011-1") =>
        Supplier.Create("Acme Supplies S.A.", documentType, taxId, null, null, null, null);

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
            supplier.Id, "Renamed Supplies S.A.", DocumentType.Dui, "08765432-1", "John Roe", "7777-7777", "new@acme.test", "New address 5");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        var persisted = await db.Suppliers.SingleAsync(s => s.Id == supplier.Id);
        Assert.Equal("Renamed Supplies S.A.", persisted.Name);
        Assert.Equal(DocumentType.Dui, persisted.DocumentType);
        Assert.Equal("08765432-1", persisted.TaxId);
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
            Guid.NewGuid(), "Renamed Supplies S.A.", DocumentType.Nit, "06140101999999", null, null, null, null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SupplierErrorCodes.NotFound, result.Error);
    }

    /// <summary>Using the document of another supplier of the same type returns the duplicate error.</summary>
    [Fact]
    public async Task HandleAsync_WithDocumentOfAnotherSupplier_ReturnsFailure()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var first = NewSupplier(DocumentType.Nit, "0614-010101-011-1");
        var second = NewSupplier(DocumentType.Nit, "0614-010199-999-9");
        db.Suppliers.AddRange(first, second);
        await db.SaveChangesAsync();

        var handler = new UpdateSupplierHandler(db);
        var command = new UpdateSupplierCommand(
            second.Id, "Renamed Supplies S.A.", DocumentType.Nit, "0614-010101-011-1", null, null, null, null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SupplierErrorCodes.TaxIdAlreadyExists, result.Error);
    }
}
