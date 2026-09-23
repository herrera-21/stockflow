using StockFlow.Domain.Entities;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the <see cref="Customer"/> aggregate invariants.
/// </summary>
public class CustomerTests
{
    // Builds a valid customer, letting each test override only the values it cares about.
    private static Customer CreateCustomer(
        string name = "Acme S.A.",
        string taxId = "3-101-123456",
        string? phone = "8888-8888",
        string? email = "contact@acme.test",
        string? address = "Main street 1") =>
        Customer.Create(name, taxId, phone, email, address);

    /// <summary>Create with valid data sets the properties and marks the customer active.</summary>
    [Fact]
    public void Create_WithValidData_SetsPropertiesAndActiveState()
    {
        // Act
        var customer = CreateCustomer();

        // Assert
        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("Acme S.A.", customer.Name);
        Assert.Equal("3-101-123456", customer.TaxId);
        Assert.Equal("8888-8888", customer.Phone);
        Assert.Equal("contact@acme.test", customer.Email);
        Assert.Equal("Main street 1", customer.Address);
        Assert.True(customer.IsActive);
    }

    /// <summary>Create trims text fields and turns blank optional values into null.</summary>
    [Fact]
    public void Create_NormalizesTextFields()
    {
        // Act
        var customer = CreateCustomer(
            name: "  Acme S.A.  ",
            taxId: "  3-101-123456  ",
            phone: "   ",
            email: string.Empty,
            address: null);

        // Assert
        Assert.Equal("Acme S.A.", customer.Name);
        Assert.Equal("3-101-123456", customer.TaxId);
        Assert.Null(customer.Phone);
        Assert.Null(customer.Email);
        Assert.Null(customer.Address);
    }

    /// <summary>Create with a blank name throws a <see cref="DomainException"/>.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsDomainException(string name)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => CreateCustomer(name: name));
    }

    /// <summary>Create with a blank tax identification throws a <see cref="DomainException"/>.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTaxId_ThrowsDomainException(string taxId)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => CreateCustomer(taxId: taxId));
    }

    /// <summary>Update changes editable data and normalizes optional values.</summary>
    [Fact]
    public void Update_ChangesEditableData()
    {
        // Arrange
        var customer = CreateCustomer();

        // Act
        customer.Update("Renamed S.A.", "3-101-999999", "7777-7777", null, "New address 5");

        // Assert
        Assert.Equal("Renamed S.A.", customer.Name);
        Assert.Equal("3-101-999999", customer.TaxId);
        Assert.Equal("7777-7777", customer.Phone);
        Assert.Null(customer.Email);
        Assert.Equal("New address 5", customer.Address);
    }

    /// <summary>Update with a blank name throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Update_WithBlankName_ThrowsDomainException()
    {
        // Arrange
        var customer = CreateCustomer();

        // Act & Assert
        Assert.Throws<DomainException>(() => customer.Update(" ", "3-101-123456", null, null, null));
    }

    /// <summary>Deactivate then Activate toggles the active state.</summary>
    [Fact]
    public void Deactivate_ThenActivate_TogglesActiveState()
    {
        // Arrange
        var customer = CreateCustomer();

        // Act
        customer.Deactivate();
        var afterDeactivate = customer.IsActive;
        customer.Activate();

        // Assert
        Assert.False(afterDeactivate);
        Assert.True(customer.IsActive);
    }
}
