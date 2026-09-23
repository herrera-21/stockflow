using StockFlow.Application.Products;
using StockFlow.Application.Products.Queries;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Tests.Products;

/// <summary>
/// Unit tests for <see cref="GetProductsPagedHandler"/>: ordering, paging, low-stock flag and the
/// optional search/category filters.
/// </summary>
public class GetProductsPagedHandlerTests
{
    // Builds a product whose name is used as SKU unless one is provided.
    private static Product NewProduct(string name, string? sku = null, string category = "General") =>
        Product.Create(sku ?? name, name, null, category, 10m, 15m, 13m, 5, 2);

    /// <summary>The requested page is ordered by name and reports the correct paging metadata.</summary>
    [Fact]
    public async Task HandleAsync_ReturnsRequestedPageOrderedByName()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Products.AddRange(NewProduct("C product"), NewProduct("A product"), NewProduct("B product"));
        await db.SaveChangesAsync();

        var handler = new GetProductsPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetProductsPagedQuery(1, 2));

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("A product", result.Items[0].Name);
        Assert.Equal("B product", result.Items[1].Name);
    }

    /// <summary>The second page returns the remaining items and navigation flags.</summary>
    [Fact]
    public async Task HandleAsync_SecondPage_ReturnsRemainingItems()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Products.AddRange(NewProduct("A product"), NewProduct("B product"), NewProduct("C product"));
        await db.SaveChangesAsync();

        var handler = new GetProductsPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetProductsPagedQuery(2, 2));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("C product", result.Items[0].Name);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    /// <summary>Products at or below their minimum stock are flagged as low stock.</summary>
    [Fact]
    public async Task HandleAsync_FlagsLowStockProducts()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Products.Add(Product.Create("SKU-001", "Low", null, "General", 10m, 15m, 13m, 1, 2));
        await db.SaveChangesAsync();

        var handler = new GetProductsPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetProductsPagedQuery(1, 10));

        // Assert
        Assert.True(result.Items[0].HasLowStock);
    }

    /// <summary>Search matches the name case-insensitively.</summary>
    [Fact]
    public async Task HandleAsync_FiltersByNameCaseInsensitive()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Products.AddRange(
            NewProduct("Wireless mouse"),
            NewProduct("Wired keyboard"),
            NewProduct("Monitor"));
        await db.SaveChangesAsync();

        var handler = new GetProductsPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetProductsPagedQuery(1, 10, Search: "WIRELESS"));

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Wireless mouse", result.Items[0].Name);
    }

    /// <summary>Search matches the SKU case-insensitively.</summary>
    [Fact]
    public async Task HandleAsync_FiltersBySku()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Products.AddRange(
            NewProduct("Mouse", sku: "SKU-AAA-001"),
            NewProduct("Keyboard", sku: "SKU-BBB-002"));
        await db.SaveChangesAsync();

        var handler = new GetProductsPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetProductsPagedQuery(1, 10, Search: "bbb"));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Keyboard", result.Items[0].Name);
    }

    /// <summary>The category filter matches case-insensitively.</summary>
    [Fact]
    public async Task HandleAsync_FiltersByCategory()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Products.AddRange(
            NewProduct("Mouse", category: "Hardware"),
            NewProduct("License", category: "Software"),
            NewProduct("Monitor", category: "Hardware"));
        await db.SaveChangesAsync();

        var handler = new GetProductsPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetProductsPagedQuery(1, 10, Category: "hardware"));

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal("Hardware", item.Category));
    }

    /// <summary>Search and category are combined with an AND.</summary>
    [Fact]
    public async Task HandleAsync_CombinesSearchAndCategory()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Products.AddRange(
            NewProduct("Wireless mouse", sku: "MOU-1", category: "Hardware"),
            NewProduct("Wireless license", sku: "LIC-1", category: "Software"),
            NewProduct("Wired mouse", sku: "MOU-2", category: "Hardware"));
        await db.SaveChangesAsync();

        var handler = new GetProductsPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(
            new GetProductsPagedQuery(1, 10, Search: "wireless", Category: "Hardware"));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Wireless mouse", result.Items[0].Name);
    }

    /// <summary>No matches produce an empty page with zero totals.</summary>
    [Fact]
    public async Task HandleAsync_WithNoMatches_ReturnsEmptyPage()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Products.Add(NewProduct("Mouse"));
        await db.SaveChangesAsync();

        var handler = new GetProductsPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetProductsPagedQuery(1, 10, Search: "does-not-exist"));

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
        db.Products.AddRange(
            NewProduct("Widget A", sku: "W-1", category: "Hardware"),
            NewProduct("Widget B", sku: "W-2", category: "Hardware"),
            NewProduct("Widget C", sku: "W-3", category: "Hardware"),
            NewProduct("Other", sku: "O-1", category: "Software"));
        await db.SaveChangesAsync();

        var handler = new GetProductsPagedHandler(db);

        // Act
        var firstPage = await handler.HandleAsync(new GetProductsPagedQuery(1, 2, Category: "Hardware"));
        var secondPage = await handler.HandleAsync(new GetProductsPagedQuery(2, 2, Category: "Hardware"));

        // Assert
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.True(firstPage.HasNextPage);
        Assert.Single(secondPage.Items);
        Assert.Equal("Widget C", secondPage.Items[0].Name);
        Assert.False(secondPage.HasNextPage);
    }
}
