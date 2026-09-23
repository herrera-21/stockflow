using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Customers;
using StockFlow.Application.Customers.Commands;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Customers;

/// <summary>
/// Unit tests for <see cref="UpdateCustomerHandler"/>.
/// </summary>
public class UpdateCustomerHandlerTests
{
    // Builds a persisted customer for the update scenarios.
    private static Customer NewCustomer(string taxId = "3-101-123456") =>
        Customer.Create("Acme S.A.", taxId, null, null, null);

    /// <summary>Updating with valid data changes the editable fields.</summary>
    [Fact]
    public async Task HandleAsync_WithValidData_UpdatesEditableFields()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var customer = NewCustomer();
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var handler = new UpdateCustomerHandler(db);
        var command = new UpdateCustomerCommand(
            customer.Id, "Renamed S.A.", "3-101-999999", "7777-7777", "new@acme.test", "New address 5");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        var persisted = await db.Customers.SingleAsync(c => c.Id == customer.Id);
        Assert.Equal("Renamed S.A.", persisted.Name);
        Assert.Equal("3-101-999999", persisted.TaxId);
        Assert.Equal("7777-7777", persisted.Phone);
        Assert.Equal("new@acme.test", persisted.Email);
        Assert.Equal("New address 5", persisted.Address);
    }

    /// <summary>Updating an unknown customer returns the not-found error code.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateCustomerHandler(db);
        var command = new UpdateCustomerCommand(
            Guid.NewGuid(), "Renamed S.A.", "3-101-999999", null, null, null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrorCodes.NotFound, result.Error);
    }

    /// <summary>Using the tax id of another customer returns the duplicate error code.</summary>
    [Fact]
    public async Task HandleAsync_WithTaxIdOfAnotherCustomer_ReturnsFailure()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var first = NewCustomer("3-101-123456");
        var second = NewCustomer("3-101-999999");
        db.Customers.AddRange(first, second);
        await db.SaveChangesAsync();

        var handler = new UpdateCustomerHandler(db);
        var command = new UpdateCustomerCommand(
            second.Id, "Renamed S.A.", "3-101-123456", null, null, null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrorCodes.TaxIdAlreadyExists, result.Error);
    }
}
