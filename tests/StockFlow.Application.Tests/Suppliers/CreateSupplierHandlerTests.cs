using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Suppliers;
using StockFlow.Application.Suppliers.Commands;

namespace StockFlow.Application.Tests.Suppliers;

/// <summary>
/// Unit tests for <see cref="CreateSupplierHandler"/>.
/// </summary>
public class CreateSupplierHandlerTests
{
    // Builds a valid command, letting each test override the tax id when needed.
    private static CreateSupplierCommand ValidCommand(string taxId = "3-101-654321") => new(
        "Acme Supplies S.A.",
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
        Assert.Equal("Jane Doe", result.Value.ContactName);
        Assert.True(result.Value.IsActive);
        Assert.True(await db.Suppliers.AnyAsync(s => s.Id == result.Value.Id));
    }

    /// <summary>Creating with a duplicate tax id fails and does not persist a second supplier.</summary>
    [Fact]
    public async Task HandleAsync_WithDuplicateTaxId_ReturnsFailure()
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
}
