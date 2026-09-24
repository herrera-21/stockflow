using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Categories;
using StockFlow.Application.Categories.Commands;

namespace StockFlow.Application.Tests.Categories;

/// <summary>
/// Unit tests for <see cref="UpdateCategoryHandler"/>.
/// </summary>
public class UpdateCategoryHandlerTests
{
    /// <summary>Renaming with a valid name updates the category.</summary>
    [Fact]
    public async Task HandleAsync_WithValidName_RenamesCategory()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var category = db.Categories.Single(c => c.Code == "cleaning");
        var handler = new UpdateCategoryHandler(db);

        // Act
        var result = await handler.HandleAsync(new UpdateCategoryCommand(category.Id, "Aseo general"));

        // Assert
        Assert.True(result.IsSuccess);
        var persisted = await db.Categories.SingleAsync(c => c.Id == category.Id);
        Assert.Equal("Aseo general", persisted.Name);
    }

    /// <summary>Renaming an unknown category returns the not-found error code.</summary>
    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateCategoryHandler(db);

        // Act
        var result = await handler.HandleAsync(new UpdateCategoryCommand(Guid.NewGuid(), "Aseo"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryErrorCodes.NotFound, result.Error);
    }

    /// <summary>Renaming to the name of another category fails.</summary>
    [Fact]
    public async Task HandleAsync_WithNameOfAnotherCategory_ReturnsFailure()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var cleaning = db.Categories.Single(c => c.Code == "cleaning");
        var handler = new UpdateCategoryHandler(db);

        // Act
        var result = await handler.HandleAsync(new UpdateCategoryCommand(cleaning.Id, "Bebidas"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryErrorCodes.NameAlreadyExists, result.Error);
    }
}
