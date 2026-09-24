using StockFlow.Domain;
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
        DocumentType documentType = DocumentType.Dui,
        string taxId = "01234567-8",
        string? phone = "8888-8888",
        string? email = "contact@acme.test",
        string? address = "Main street 1") =>
        Customer.Create(name, documentType, taxId, phone, email, address);

    /// <summary>Create with valid data sets the properties and marks the customer active.</summary>
    [Fact]
    public void Create_WithValidData_SetsPropertiesAndActiveState()
    {
        // Act
        var customer = CreateCustomer();

        // Assert
        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("Acme S.A.", customer.Name);
        Assert.Equal(DocumentType.Dui, customer.DocumentType);
        Assert.Equal("01234567-8", customer.TaxId);
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
            taxId: "  01234567-8  ",
            phone: "   ",
            email: string.Empty,
            address: null);

        // Assert
        Assert.Equal("Acme S.A.", customer.Name);
        Assert.Equal("01234567-8", customer.TaxId);
        Assert.Null(customer.Phone);
        Assert.Null(customer.Email);
        Assert.Null(customer.Address);
    }

    /// <summary>Create with a 14-digit NIT formats it with hyphens.</summary>
    [Fact]
    public void Create_WithCompanyNit_FormatsDocument()
    {
        // Act
        var customer = CreateCustomer(documentType: DocumentType.Nit, taxId: "06140101011011");

        // Assert
        Assert.Equal("0614-010101-101-1", customer.TaxId);
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

    /// <summary>A customer name may contain digits and symbols because it can be a company.</summary>
    [Theory]
    [InlineData("747")]
    [InlineData("Maximo123")]
    [InlineData("Acme & Co")]
    [InlineData("Distribuidora 24/7 S.A. de C.V.")]
    public void Create_WithCompanyNameContainingDigitsAndSymbols_IsAllowed(string name)
    {
        // Act
        var customer = CreateCustomer(name: name);

        // Assert
        Assert.Equal(name, customer.Name);
    }

    /// <summary>Create with a blank document number throws a <see cref="DomainException"/>.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTaxId_ThrowsDomainException(string taxId)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => CreateCustomer(taxId: taxId));
    }

    /// <summary>Create with a document that does not match the type throws.</summary>
    [Theory]
    [InlineData(DocumentType.Dui, "ABC")]
    [InlineData(DocumentType.Nit, "123")]
    public void Create_WithInvalidDocument_ThrowsDomainException(DocumentType documentType, string taxId)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => CreateCustomer(documentType: documentType, taxId: taxId));
    }

    /// <summary>Create with an invalid phone number throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Create_WithInvalidPhone_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => CreateCustomer(phone: "call-me"));
    }

    /// <summary>Update changes editable data and normalizes optional values.</summary>
    [Fact]
    public void Update_ChangesEditableData()
    {
        // Arrange
        var customer = CreateCustomer();

        // Act
        customer.Update("Renamed S.A.", DocumentType.Nit, "06140101999999", "7777-7777", null, "New address 5");

        // Assert
        Assert.Equal("Renamed S.A.", customer.Name);
        Assert.Equal(DocumentType.Nit, customer.DocumentType);
        Assert.Equal("0614-010199-999-9", customer.TaxId);
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
        Assert.Throws<DomainException>(() => customer.Update(" ", DocumentType.Dui, "01234567-8", null, null, null));
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
