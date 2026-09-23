using StockFlow.Application.Suppliers.Queries;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Suppliers;

/// <summary>
/// Unit tests for <see cref="GetSuppliersPagedHandler"/>: ordering, paging and the optional search
/// filter across name, tax identification, contact name and email.
/// </summary>
public class GetSuppliersPagedHandlerTests
{
    // Builds a supplier whose name is used as tax id unless one is provided.
    private static Supplier NewSupplier(
        string name,
        string? taxId = null,
        string? contactName = null,
        string? email = null) =>
        Supplier.Create(name, taxId ?? name, contactName, null, email, null);

    /// <summary>The requested page is ordered by name and reports the correct paging metadata.</summary>
    [Fact]
    public async Task HandleAsync_ReturnsRequestedPageOrderedByName()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Suppliers.AddRange(NewSupplier("C supplier"), NewSupplier("A supplier"), NewSupplier("B supplier"));
        await db.SaveChangesAsync();

        var handler = new GetSuppliersPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetSuppliersPagedQuery(1, 2));

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("A supplier", result.Items[0].Name);
        Assert.Equal("B supplier", result.Items[1].Name);
    }

    /// <summary>The second page returns the remaining items and navigation flags.</summary>
    [Fact]
    public async Task HandleAsync_SecondPage_ReturnsRemainingItems()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Suppliers.AddRange(NewSupplier("A supplier"), NewSupplier("B supplier"), NewSupplier("C supplier"));
        await db.SaveChangesAsync();

        var handler = new GetSuppliersPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetSuppliersPagedQuery(2, 2));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("C supplier", result.Items[0].Name);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    /// <summary>Search matches the name case-insensitively.</summary>
    [Fact]
    public async Task HandleAsync_FiltersByNameCaseInsensitive()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Suppliers.AddRange(
            NewSupplier("Acme Supplies"),
            NewSupplier("Globex"),
            NewSupplier("Initech"));
        await db.SaveChangesAsync();

        var handler = new GetSuppliersPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetSuppliersPagedQuery(1, 10, Search: "ACME"));

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Acme Supplies", result.Items[0].Name);
    }

    /// <summary>Search matches the tax identification case-insensitively.</summary>
    [Fact]
    public async Task HandleAsync_FiltersByTaxId()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Suppliers.AddRange(
            NewSupplier("Acme", taxId: "3-101-AAAA"),
            NewSupplier("Globex", taxId: "3-101-BBBB"));
        await db.SaveChangesAsync();

        var handler = new GetSuppliersPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetSuppliersPagedQuery(1, 10, Search: "bbbb"));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Globex", result.Items[0].Name);
    }

    /// <summary>Search matches the contact name case-insensitively and ignores null contact names.</summary>
    [Fact]
    public async Task HandleAsync_FiltersByContactName()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Suppliers.AddRange(
            NewSupplier("Acme", contactName: "Jane Doe"),
            NewSupplier("Globex", contactName: "John Roe"),
            NewSupplier("NoContact", taxId: "3-101-CCCC"));
        await db.SaveChangesAsync();

        var handler = new GetSuppliersPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetSuppliersPagedQuery(1, 10, Search: "jane"));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Acme", result.Items[0].Name);
    }

    /// <summary>Search matches the email case-insensitively and ignores null emails.</summary>
    [Fact]
    public async Task HandleAsync_FiltersByEmail()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Suppliers.AddRange(
            NewSupplier("Acme", email: "billing@acme.test"),
            NewSupplier("Globex", email: "contact@globex.test"),
            NewSupplier("NoEmail", taxId: "3-101-CCCC"));
        await db.SaveChangesAsync();

        var handler = new GetSuppliersPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetSuppliersPagedQuery(1, 10, Search: "globex.test"));

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
        db.Suppliers.Add(NewSupplier("Acme"));
        await db.SaveChangesAsync();

        var handler = new GetSuppliersPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetSuppliersPagedQuery(1, 10, Search: "does-not-exist"));

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
        db.Suppliers.AddRange(
            NewSupplier("Widget A"),
            NewSupplier("Widget B"),
            NewSupplier("Widget C"),
            NewSupplier("Other"));
        await db.SaveChangesAsync();

        var handler = new GetSuppliersPagedHandler(db);

        // Act
        var firstPage = await handler.HandleAsync(new GetSuppliersPagedQuery(1, 2, Search: "Widget"));
        var secondPage = await handler.HandleAsync(new GetSuppliersPagedQuery(2, 2, Search: "Widget"));

        // Assert
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.True(firstPage.HasNextPage);
        Assert.Single(secondPage.Items);
        Assert.Equal("Widget C", secondPage.Items[0].Name);
        Assert.False(secondPage.HasNextPage);
    }
}
