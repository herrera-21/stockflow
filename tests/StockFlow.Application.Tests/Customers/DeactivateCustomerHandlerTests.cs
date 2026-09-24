using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Customers;
using StockFlow.Application.Customers.Commands;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Customers;

/// <summary>
/// Unit tests for <see cref="DeactivateCustomerHandler"/>.
/// </summary>
public class DeactivateCustomerHandlerTests
{
    /// <summary>Deactivating an existing customer marks it inactive without deleting it.</summary>
    [Fact]
    public async Task HandleAsync_WithExistingCustomer_DeactivatesWithoutDeleting()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var customer = Customer.Create("Acme S.A.", DocumentType.Dui, "01234567-8", null, null, null);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var handler = new DeactivateCustomerHandler(db);

        // Act
        var result = await handler.HandleAsync(new DeactivateCustomerCommand(customer.Id));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsActive);
        var persisted = await db.Customers.SingleAsync(c => c.Id == customer.Id);
        Assert.False(persisted.IsActive);
    }

    /// <summary>Deactivating an unknown customer returns the not-found error code.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new DeactivateCustomerHandler(db);

        // Act
        var result = await handler.HandleAsync(new DeactivateCustomerCommand(Guid.NewGuid()));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrorCodes.NotFound, result.Error);
    }
}
