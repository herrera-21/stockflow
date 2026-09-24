using StockFlow.Application.Customers.Queries;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Customers;

/// <summary>
/// Unit tests for <see cref="GetCustomersPagedHandler"/>: ordering, paging and the optional search
/// filter across name, tax identification and email.
/// </summary>
public class GetCustomersPagedHandlerTests
{
    // Builds a customer with a valid DUI, letting each test override name, document or email.
    private static Customer NewCustomer(string name, string taxId = "01234567-8", string? email = null) =>
        Customer.Create(name, DocumentType.Dui, taxId, null, email, null);

    /// <summary>The requested page is ordered by name and reports the correct paging metadata.</summary>
    [Fact]
    public async Task HandleAsync_ReturnsRequestedPageOrderedByName()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Customers.AddRange(
            NewCustomer("C customer", "00000003-3"),
            NewCustomer("A customer", "00000001-1"),
            NewCustomer("B customer", "00000002-2"));
        await db.SaveChangesAsync();

        var handler = new GetCustomersPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetCustomersPagedQuery(1, 2));

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("A customer", result.Items[0].Name);
        Assert.Equal("B customer", result.Items[1].Name);
    }

    /// <summary>The second page returns the remaining items and navigation flags.</summary>
    [Fact]
    public async Task HandleAsync_SecondPage_ReturnsRemainingItems()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Customers.AddRange(
            NewCustomer("A customer", "00000001-1"),
            NewCustomer("B customer", "00000002-2"),
            NewCustomer("C customer", "00000003-3"));
        await db.SaveChangesAsync();

        var handler = new GetCustomersPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetCustomersPagedQuery(2, 2));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("C customer", result.Items[0].Name);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    /// <summary>Search matches the name case-insensitively.</summary>
    [Fact]
    public async Task HandleAsync_FiltersByNameCaseInsensitive()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Customers.AddRange(
            NewCustomer("Acme Corporation", "00000001-1"),
            NewCustomer("Globex", "00000002-2"),
            NewCustomer("Initech", "00000003-3"));
        await db.SaveChangesAsync();

        var handler = new GetCustomersPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetCustomersPagedQuery(1, 10, Search: "ACME"));

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Acme Corporation", result.Items[0].Name);
    }

    /// <summary>Search matches the document number.</summary>
    [Fact]
    public async Task HandleAsync_FiltersByTaxId()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Customers.AddRange(
            NewCustomer("Acme", "01234567-8"),
            NewCustomer("Globex", "08765432-1"));
        await db.SaveChangesAsync();

        var handler = new GetCustomersPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetCustomersPagedQuery(1, 10, Search: "8765"));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Globex", result.Items[0].Name);
    }

    /// <summary>Search matches the email case-insensitively and ignores null emails.</summary>
    [Fact]
    public async Task HandleAsync_FiltersByEmail()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Customers.AddRange(
            NewCustomer("Acme", "00000001-1", "billing@acme.test"),
            NewCustomer("Globex", "00000002-2", "contact@globex.test"),
            NewCustomer("NoEmail", "00000003-3"));
        await db.SaveChangesAsync();

        var handler = new GetCustomersPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetCustomersPagedQuery(1, 10, Search: "globex.test"));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Globex", result.Items[0].Name);
    }

    /// <summary>No matches produce an empty page with zero totals.</summary>
    [Fact]
    public async Task HandleAsync_WithNoMatches_ReturnsEmptyPage()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Customers.Add(NewCustomer("Acme"));
        await db.SaveChangesAsync();

        var handler = new GetCustomersPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetCustomersPagedQuery(1, 10, Search: "does-not-exist"));

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    /// <summary>Paging is applied after filtering, so totals reflect the filtered set.</summary>
    [Fact]
    public async Task HandleAsync_PagesFilteredResults()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Customers.AddRange(
            NewCustomer("Widget A", "00000001-1"),
            NewCustomer("Widget B", "00000002-2"),
            NewCustomer("Widget C", "00000003-3"),
            NewCustomer("Other", "00000004-4"));
        await db.SaveChangesAsync();

        var handler = new GetCustomersPagedHandler(db);

        // Act
        var firstPage = await handler.HandleAsync(new GetCustomersPagedQuery(1, 2, Search: "Widget"));
        var secondPage = await handler.HandleAsync(new GetCustomersPagedQuery(2, 2, Search: "Widget"));

        // Assert
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.True(firstPage.HasNextPage);
        Assert.Single(secondPage.Items);
        Assert.Equal("Widget C", secondPage.Items[0].Name);
        Assert.False(secondPage.HasNextPage);
    }
}
