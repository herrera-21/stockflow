using StockFlow.Application.Products.Queries;
using StockFlow.Domain.Entities;
using StockFlow.Domain;

namespace StockFlow.Application.Tests.Products;

/// <summary>
/// Unit tests for <see cref="GetProductsPagedHandler"/>: ordering, paging, low-stock flag and the
/// optional search/category filters.
/// </summary>
public class GetProductsPagedHandlerTests
{
    // Builds a product whose name is used as SKU unless one is provided.
    private static Product NewProduct(Guid categoryId, string name, string? sku = null) =>
        Product.Create(sku ?? name, name, null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 5, 2, "user-1", "user@test.local", DateTimeOffset.UnixEpoch);

    /// <summary>The requested page is ordered by name and reports the correct paging metadata.</summary>
    [Fact]
    public async Task HandleAsync_ReturnsRequestedPageOrderedByName()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var categoryId = db.Categories.First().Id;
        db.Products.AddRange(
            NewProduct(categoryId, "C product"),
            NewProduct(categoryId, "A product"),
            NewProduct(categoryId, "B product"));
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
        var categoryId = db.Categories.First().Id;
        db.Products.AddRange(
            NewProduct(categoryId, "A product"),
            NewProduct(categoryId, "B product"),
            NewProduct(categoryId, "C product"));
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
        var categoryId = db.Categories.First().Id;
        db.Products.Add(Product.Create("SKU-001", "Low", null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 1, 2, "user-1", "user@test.local", DateTimeOffset.UnixEpoch));
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
        var categoryId = db.Categories.First().Id;
        db.Products.AddRange(
            NewProduct(categoryId, "Wireless mouse"),
            NewProduct(categoryId, "Wired keyboard"),
            NewProduct(categoryId, "Monitor"));
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
        var categoryId = db.Categories.First().Id;
        db.Products.AddRange(
            NewProduct(categoryId, "Mouse", sku: "SKU-AAA-001"),
            NewProduct(categoryId, "Keyboard", sku: "SKU-BBB-002"));
        await db.SaveChangesAsync();

        var handler = new GetProductsPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetProductsPagedQuery(1, 10, Search: "bbb"));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Keyboard", result.Items[0].Name);
    }

    /// <summary>The category filter returns only the products of that category.</summary>
    [Fact]
    public async Task HandleAsync_FiltersByCategory()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var cleaningId = db.Categories.Single(c => c.Code == "cleaning").Id;
        var beveragesId = db.Categories.Single(c => c.Code == "beverages").Id;
        db.Products.AddRange(
            NewProduct(cleaningId, "Mouse"),
            NewProduct(beveragesId, "License"),
            NewProduct(cleaningId, "Monitor"));
        await db.SaveChangesAsync();

        var handler = new GetProductsPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetProductsPagedQuery(1, 10, CategoryId: cleaningId));

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(cleaningId, item.CategoryId));
    }

    /// <summary>Search and category are combined with an AND.</summary>
    [Fact]
    public async Task HandleAsync_CombinesSearchAndCategory()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var cleaningId = db.Categories.Single(c => c.Code == "cleaning").Id;
        var beveragesId = db.Categories.Single(c => c.Code == "beverages").Id;
        db.Products.AddRange(
            NewProduct(cleaningId, "Wireless mouse", sku: "MOU-1"),
            NewProduct(beveragesId, "Wireless license", sku: "LIC-1"),
            NewProduct(cleaningId, "Wired mouse", sku: "MOU-2"));
        await db.SaveChangesAsync();

        var handler = new GetProductsPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(
            new GetProductsPagedQuery(1, 10, Search: "wireless", CategoryId: cleaningId));

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
        var categoryId = db.Categories.First().Id;
        db.Products.Add(NewProduct(categoryId, "Mouse"));
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
        var cleaningId = db.Categories.Single(c => c.Code == "cleaning").Id;
        var beveragesId = db.Categories.Single(c => c.Code == "beverages").Id;
        db.Products.AddRange(
            NewProduct(cleaningId, "Widget A", sku: "W-1"),
            NewProduct(cleaningId, "Widget B", sku: "W-2"),
            NewProduct(cleaningId, "Widget C", sku: "W-3"),
            NewProduct(beveragesId, "Other", sku: "O-1"));
        await db.SaveChangesAsync();

        var handler = new GetProductsPagedHandler(db);

        // Act
        var firstPage = await handler.HandleAsync(new GetProductsPagedQuery(1, 2, CategoryId: cleaningId));
        var secondPage = await handler.HandleAsync(new GetProductsPagedQuery(2, 2, CategoryId: cleaningId));

        // Assert
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.True(firstPage.HasNextPage);
        Assert.Single(secondPage.Items);
        Assert.Equal("Widget C", secondPage.Items[0].Name);
        Assert.False(secondPage.HasNextPage);
    }
}
