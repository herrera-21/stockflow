using StockFlow.Domain.Entities;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the <see cref="Category"/> aggregate invariants.
/// </summary>
public class CategoryTests
{
    /// <summary>Create with a valid name sets the properties and marks the category active.</summary>
    [Fact]
    public void Create_WithValidName_SetsPropertiesAndActiveState()
    {
        // Act
        var category = Category.Create("Limpieza");

        // Assert
        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal("Limpieza", category.Name);
        Assert.Null(category.Code);
        Assert.True(category.IsActive);
    }

    /// <summary>Create with a code keeps it, used to look up the localized name.</summary>
    [Fact]
    public void Create_WithCode_KeepsCode()
    {
        // Act
        var category = Category.Create("  Limpieza  ", "cleaning");

        // Assert
        Assert.Equal("Limpieza", category.Name);
        Assert.Equal("cleaning", category.Code);
    }

    /// <summary>Create with a blank name throws a <see cref="DomainException"/>.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsDomainException(string name)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Category.Create(name));
    }

    /// <summary>Rename changes the display name.</summary>
    [Fact]
    public void Rename_ChangesName()
    {
        // Arrange
        var category = Category.Create("Limpieza");

        // Act
        category.Rename("Aseo");

        // Assert
        Assert.Equal("Aseo", category.Name);
    }

    /// <summary>Rename with a blank name throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Rename_WithBlankName_ThrowsDomainException()
    {
        // Arrange
        var category = Category.Create("Limpieza");

        // Act & Assert
        Assert.Throws<DomainException>(() => category.Rename(" "));
    }

    /// <summary>Deactivate then Activate toggles the active state.</summary>
    [Fact]
    public void Deactivate_ThenActivate_TogglesActiveState()
    {
        // Arrange
        var category = Category.Create("Limpieza");

        // Act
        category.Deactivate();
        var afterDeactivate = category.IsActive;
        category.Activate();

        // Assert
        Assert.False(afterDeactivate);
        Assert.True(category.IsActive);
    }
}
