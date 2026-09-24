using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Suppliers;
using StockFlow.Application.Suppliers.Commands;
using StockFlow.Domain;

namespace StockFlow.Application.Tests.Suppliers;

/// <summary>
/// Unit tests for <see cref="CreateSupplierHandler"/>.
/// </summary>
public class CreateSupplierHandlerTests
{
    // Builds a valid command, letting each test override the document when needed.
    private static CreateSupplierCommand ValidCommand(
        DocumentType documentType = DocumentType.Nit,
        string taxId = "0614-010101-011-1") => new(
        "Acme Supplies S.A.",
        documentType,
        taxId,
        "Jane Doe",
        "8888-8888",
        "sales@acme.test",
        "Main street 1");

    /// <summary>Creating with valid data persists an active supplier.</summary>
    [Fact]
    public async Task HandleAsync_WithValidData_PersistsSupplier()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateSupplierHandler(db);

        // Act
        var result = await handler.HandleAsync(ValidCommand());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Acme Supplies S.A.", result.Value!.Name);
        Assert.Equal(DocumentType.Nit, result.Value.DocumentType);
        Assert.Equal("Jane Doe", result.Value.ContactName);
        Assert.True(result.Value.IsActive);
        Assert.True(await db.Suppliers.AnyAsync(s => s.Id == result.Value.Id));
    }

    /// <summary>Creating with a duplicate document of the same type fails.</summary>
    [Fact]
    public async Task HandleAsync_WithDuplicateDocument_ReturnsFailure()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateSupplierHandler(db);
        await handler.HandleAsync(ValidCommand());

        // Act
        var result = await handler.HandleAsync(ValidCommand());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SupplierErrorCodes.TaxIdAlreadyExists, result.Error);
        Assert.Equal(1, await db.Suppliers.CountAsync());
    }

    /// <summary>The same number under a different document type is allowed.</summary>
    [Fact]
    public async Task HandleAsync_WithSameNumberDifferentType_Succeeds()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateSupplierHandler(db);
        await handler.HandleAsync(ValidCommand(DocumentType.Dui, "01234567-8"));

        // Act
        var result = await handler.HandleAsync(ValidCommand(DocumentType.Nit, "01234567-8"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, await db.Suppliers.CountAsync());
    }
}
