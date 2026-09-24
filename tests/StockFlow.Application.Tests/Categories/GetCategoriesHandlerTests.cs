using StockFlow.Application.Categories.Queries;
using StockFlow.Domain;

namespace StockFlow.Application.Tests.Categories;

/// <summary>
/// Unit tests for <see cref="GetCategoriesHandler"/> and <see cref="GetCategoriesPagedHandler"/>.
/// </summary>
public class GetCategoriesHandlerTests
{
    /// <summary>Only active categories are returned, ordered by name.</summary>
    [Fact]
    public async Task GetCategories_ReturnsOnlyActiveOrderedByName()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        db.Categories.Single(c => c.Code == "beverages").Deactivate();
        await db.SaveChangesAsync();

        var handler = new GetCategoriesHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetCategoriesQuery());

        // Assert
        Assert.DoesNotContain(result, c => c.Code == "beverages");
        Assert.Equal(result.OrderBy(c => c.Name).Select(c => c.Name), result.Select(c => c.Name));
    }

    /// <summary>The paged query filters by name and reports the product count.</summary>
    [Fact]
    public async Task GetCategoriesPaged_FiltersByNameAndCountsProducts()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var cleaning = db.Categories.Single(c => c.Code == "cleaning");
        db.Products.Add(Domain.Entities.Product.Create("SKU-001", "P1", null, cleaning.Id, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 1, 0, "user-1", "user@test.local", DateTimeOffset.UnixEpoch));
        db.Products.Add(Domain.Entities.Product.Create("SKU-002", "P2", null, cleaning.Id, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 1, 0, "user-1", "user@test.local", DateTimeOffset.UnixEpoch));
        await db.SaveChangesAsync();

        var handler = new GetCategoriesPagedHandler(db);

        // Act
        var result = await handler.HandleAsync(new GetCategoriesPagedQuery(1, 10, Search: "limp"));

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("cleaning", result.Items[0].Code);
        Assert.Equal(2, result.Items[0].ProductCount);
    }
}
