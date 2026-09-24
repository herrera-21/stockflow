using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Categories;
using StockFlow.Application.Categories.Commands;

namespace StockFlow.Application.Tests.Categories;

/// <summary>
/// Unit tests for <see cref="CreateCategoryHandler"/>.
/// </summary>
public class CreateCategoryHandlerTests
{
    /// <summary>Creating with a valid name persists an active category.</summary>
    [Fact]
    public async Task HandleAsync_WithValidName_PersistsCategory()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateCategoryHandler(db);

        // Act
        var result = await handler.HandleAsync(new CreateCategoryCommand("Ferretería"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Ferretería", result.Value!.Name);
        Assert.True(result.Value.IsActive);
        Assert.True(await db.Categories.AnyAsync(c => c.Id == result.Value.Id));
    }

    /// <summary>Creating with a name that only differs in case fails.</summary>
    [Fact]
    public async Task HandleAsync_WithDuplicateNameIgnoringCase_ReturnsFailure()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateCategoryHandler(db);
        await handler.HandleAsync(new CreateCategoryCommand("Ferretería"));

        // Act
        var result = await handler.HandleAsync(new CreateCategoryCommand("ferretería"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(CategoryErrorCodes.NameAlreadyExists, result.Error);
    }
}
