using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Categories;
using StockFlow.Application.Categories.Commands;
using StockFlow.Domain.Entities;
using StockFlow.Domain;

namespace StockFlow.Application.Tests.Categories;

/// <summary>
/// Unit tests for <see cref="DeactivateCategoryHandler"/> and <see cref="ActivateCategoryHandler"/>.
/// </summary>
public class DeactivateCategoryHandlerTests
{
    /// <summary>Deactivating an unused category marks it inactive.</summary>
    [Fact]
    public async Task Deactivate_WithNoProducts_Deactivates()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var category = db.Categories.Single(c => c.Code == "cleaning");
        var handler = new DeactivateCategoryHandler(db);

        // Act
        var result = await handler.HandleAsync(new DeactivateCategoryCommand(category.Id));

        // Assert
        Assert.True(result.IsSuccess);
        var persisted = await db.Categories.SingleAsync(c => c.Id == category.Id);
        Assert.False(persisted.IsActive);
    }

    /// <summary>Deactivating a category with associated products is refused.</summary>
    [Fact]
    public async Task Deactivate_WithProducts_ReturnsInUse()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var category = db.Categories.Single(c => c.Code == "cleaning");
        db.Products.Add(Product.Create("SKU-001", "Test product", null, category.Id, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 1, 0));
        await db.SaveChangesAsync();

        var handler = new DeactivateCategoryHandler(db);

        // Act
        var result = await handler.HandleAsync(new DeactivateCategoryCommand(category.Id));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryErrorCodes.InUse, result.Error);
        var persisted = await db.Categories.SingleAsync(c => c.Id == category.Id);
        Assert.True(persisted.IsActive);
    }

    /// <summary>Deactivating an unknown category returns the not-found error code.</summary>
    [Fact]
    public async Task Deactivate_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new DeactivateCategoryHandler(db);

        // Act
        var result = await handler.HandleAsync(new DeactivateCategoryCommand(Guid.NewGuid()));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryErrorCodes.NotFound, result.Error);
    }

    /// <summary>Activating a deactivated category marks it active again.</summary>
    [Fact]
    public async Task Activate_WithExistingCategory_Activates()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var category = db.Categories.Single(c => c.Code == "cleaning");
        category.Deactivate();
        await db.SaveChangesAsync();

        var handler = new ActivateCategoryHandler(db);

        // Act
        var result = await handler.HandleAsync(new ActivateCategoryCommand(category.Id));

        // Assert
        Assert.True(result.IsSuccess);
        var persisted = await db.Categories.SingleAsync(c => c.Id == category.Id);
        Assert.True(persisted.IsActive);
    }
}
