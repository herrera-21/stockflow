using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Customers;
using StockFlow.Application.Customers.Commands;

namespace StockFlow.Application.Tests.Customers;

/// <summary>
/// Unit tests for <see cref="CreateCustomerHandler"/>.
/// </summary>
public class CreateCustomerHandlerTests
{
    // Builds a valid command, letting each test override the tax id when needed.
    private static CreateCustomerCommand ValidCommand(string taxId = "3-101-123456") => new(
        "Acme S.A.",
        taxId,
        "8888-8888",
        "contact@acme.test",
        "Main street 1");

    /// <summary>Creating with valid data persists an active customer.</summary>
    [Fact]
    public async Task HandleAsync_WithValidData_PersistsCustomer()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateCustomerHandler(db);

        // Act
        var result = await handler.HandleAsync(ValidCommand());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Acme S.A.", result.Value!.Name);
        Assert.True(result.Value.IsActive);
        Assert.True(await db.Customers.AnyAsync(c => c.Id == result.Value.Id));
    }

    /// <summary>Creating with a duplicate tax id fails and does not persist a second customer.</summary>
    [Fact]
    public async Task HandleAsync_WithDuplicateTaxId_ReturnsFailure()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateCustomerHandler(db);
        await handler.HandleAsync(ValidCommand());

        // Act
        var result = await handler.HandleAsync(ValidCommand());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrorCodes.TaxIdAlreadyExists, result.Error);
        Assert.Equal(1, await db.Customers.CountAsync());
    }
}
