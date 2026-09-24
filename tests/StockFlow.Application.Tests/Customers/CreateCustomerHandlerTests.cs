using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Customers;
using StockFlow.Application.Customers.Commands;
using StockFlow.Domain;

namespace StockFlow.Application.Tests.Customers;

/// <summary>
/// Unit tests for <see cref="CreateCustomerHandler"/>.
/// </summary>
public class CreateCustomerHandlerTests
{
    // Builds a valid command, letting each test override the document when needed.
    private static CreateCustomerCommand ValidCommand(
        DocumentType documentType = DocumentType.Dui,
        string taxId = "01234567-8") => new(
        "Acme S.A.",
        documentType,
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
        Assert.Equal(DocumentType.Dui, result.Value.DocumentType);
        Assert.True(result.Value.IsActive);
        Assert.True(await db.Customers.AnyAsync(c => c.Id == result.Value.Id));
    }

    /// <summary>Creating with a duplicate document of the same type fails.</summary>
    [Fact]
    public async Task HandleAsync_WithDuplicateDocument_ReturnsFailure()
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

    /// <summary>The same number under a different document type is allowed.</summary>
    [Fact]
    public async Task HandleAsync_WithSameNumberDifferentType_Succeeds()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateCustomerHandler(db);
        await handler.HandleAsync(ValidCommand(DocumentType.Dui, "01234567-8"));

        // Act
        var result = await handler.HandleAsync(ValidCommand(DocumentType.Nit, "01234567-8"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, await db.Customers.CountAsync());
    }
}
