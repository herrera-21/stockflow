using StockFlow.Application.Customers.Queries;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Customers;

/// <summary>
/// Unit tests for <see cref="GetCustomerByIdHandler"/>.
/// </summary>
public class GetCustomerByIdHandlerTests
{
    /// <summary>Querying an existing identifier returns the projected customer.</summary>
    [Fact]
    public async Task HandleAsync_WithExistingId_ReturnsCustomer()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var customer = Customer.Create(
            "Acme S.A.", DocumentType.Dui, "01234567-8", "8888-8888", "contact@acme.test", "Main street 1");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var handler = new GetCustomerByIdHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetCustomerByIdQuery(customer.Id));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customer.Id, result!.Id);
        Assert.Equal("Acme S.A.", result.Name);
        Assert.Equal(DocumentType.Dui, result.DocumentType);
        Assert.Equal("01234567-8", result.TaxId);
    }

    /// <summary>Querying an unknown identifier returns null.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNull()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new GetCustomerByIdHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetCustomerByIdQuery(Guid.NewGuid()));

        // Assert
        Assert.Null(result);
    }
}
