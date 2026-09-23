using StockFlow.Domain.Entities;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the <see cref="Supplier"/> aggregate invariants.
/// </summary>
public class SupplierTests
{
    // Builds a valid supplier, letting each test override only the values it cares about.
    private static Supplier CreateSupplier(
        string name = "Acme Supplies S.A.",
        string taxId = "3-101-654321",
        string? contactName = "Jane Doe",
        string? phone = "8888-8888",
        string? email = "sales@acme.test",
        string? address = "Main street 1") =>
        Supplier.Create(name, taxId, contactName, phone, email, address);

    /// <summary>Create with valid data sets the properties and marks the supplier active.</summary>
    [Fact]
    public void Create_WithValidData_SetsPropertiesAndActiveState()
    {
        // Act
        var supplier = CreateSupplier();

        // Assert
        Assert.NotEqual(Guid.Empty, supplier.Id);
        Assert.Equal("Acme Supplies S.A.", supplier.Name);
        Assert.Equal("3-101-654321", supplier.TaxId);
        Assert.Equal("Jane Doe", supplier.ContactName);
        Assert.Equal("8888-8888", supplier.Phone);
        Assert.Equal("sales@acme.test", supplier.Email);
        Assert.Equal("Main street 1", supplier.Address);
        Assert.True(supplier.IsActive);
    }

    /// <summary>Create trims text fields and turns blank optional values into null.</summary>
    [Fact]
    public void Create_NormalizesTextFields()
    {
        // Act
        var supplier = CreateSupplier(
            name: "  Acme Supplies S.A.  ",
            taxId: "  3-101-654321  ",
            contactName: "   ",
            phone: string.Empty,
            email: null,
            address: "  Main street 1  ");

        // Assert
        Assert.Equal("Acme Supplies S.A.", supplier.Name);
        Assert.Equal("3-101-654321", supplier.TaxId);
        Assert.Null(supplier.ContactName);
        Assert.Null(supplier.Phone);
        Assert.Null(supplier.Email);
        Assert.Equal("Main street 1", supplier.Address);
    }

    /// <summary>Create with a blank name throws a <see cref="DomainException"/>.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsDomainException(string name)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => CreateSupplier(name: name));
    }

    /// <summary>Create with a blank tax identification throws a <see cref="DomainException"/>.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTaxId_ThrowsDomainException(string taxId)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => CreateSupplier(taxId: taxId));
    }

    /// <summary>Update changes editable data and normalizes optional values.</summary>
    [Fact]
    public void Update_ChangesEditableData()
    {
        // Arrange
        var supplier = CreateSupplier();

        // Act
        supplier.Update("Renamed Supplies S.A.", "3-101-999999", null, "7777-7777", null, "New address 5");

        // Assert
        Assert.Equal("Renamed Supplies S.A.", supplier.Name);
        Assert.Equal("3-101-999999", supplier.TaxId);
        Assert.Null(supplier.ContactName);
        Assert.Equal("7777-7777", supplier.Phone);
        Assert.Null(supplier.Email);
        Assert.Equal("New address 5", supplier.Address);
    }

    /// <summary>Update with a blank name throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Update_WithBlankName_ThrowsDomainException()
    {
        // Arrange
        var supplier = CreateSupplier();

        // Act & Assert
        Assert.Throws<DomainException>(() => supplier.Update(" ", "3-101-654321", null, null, null, null));
    }

    /// <summary>Deactivate then Activate toggles the active state.</summary>
    [Fact]
    public void Deactivate_ThenActivate_TogglesActiveState()
    {
        // Arrange
        var supplier = CreateSupplier();

        // Act
        supplier.Deactivate();
        var afterDeactivate = supplier.IsActive;
        supplier.Activate();

        // Assert
        Assert.False(afterDeactivate);
        Assert.True(supplier.IsActive);
    }
}
