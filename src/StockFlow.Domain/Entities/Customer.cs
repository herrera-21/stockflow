using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

/// <summary>
/// Customer aggregate: general data, identity document, contact details and address. Customers are
/// never deleted physically; they are deactivated instead.
/// </summary>
public class Customer
{
    // EF Core materialization constructor.
    private Customer()
    {
    }

    private Customer(
        Guid id,
        string name,
        DocumentType documentType,
        string taxId,
        string? phone,
        string? email,
        string? address,
        bool isActive)
    {
        Id = id;
        Name = name;
        DocumentType = documentType;
        TaxId = taxId;
        Phone = phone;
        Email = email;
        Address = address;
        IsActive = isActive;
    }

    /// <summary>Unique identifier (UUID v7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Customer display name (person or company).</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Type of the identity document.</summary>
    public DocumentType DocumentType { get; private set; }

    /// <summary>Identity document number, formatted according to <see cref="DocumentType"/>.</summary>
    public string TaxId { get; private set; } = null!;

    /// <summary>Optional contact phone number.</summary>
    public string? Phone { get; private set; }

    /// <summary>Optional contact email address.</summary>
    public string? Email { get; private set; }

    /// <summary>Optional postal address.</summary>
    public string? Address { get; private set; }

    /// <summary>Whether the customer is active (soft-delete flag).</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Creates a new active customer.
    /// </summary>
    /// <param name="name">Display name; must not be blank and only letters, spaces and . ' -.</param>
    /// <param name="documentType">Type of the identity document.</param>
    /// <param name="taxId">Identity document number; must match <paramref name="documentType"/>.</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional postal address.</param>
    /// <returns>The created customer.</returns>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public static Customer Create(
        string name,
        DocumentType documentType,
        string taxId,
        string? phone,
        string? email,
        string? address)
    {
        Validate(name, documentType, taxId, phone);

        return new Customer(
            Guid.CreateVersion7(),
            name.Trim(),
            documentType,
            DocumentValidation.Normalize(documentType, taxId),
            Normalize(phone),
            Normalize(email),
            Normalize(address),
            isActive: true);
    }

    /// <summary>
    /// Updates the customer's editable data.
    /// </summary>
    /// <param name="name">Display name; must not be blank and only letters, spaces and . ' -.</param>
    /// <param name="documentType">Type of the identity document.</param>
    /// <param name="taxId">Identity document number; must match <paramref name="documentType"/>.</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional postal address.</param>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public void Update(
        string name,
        DocumentType documentType,
        string taxId,
        string? phone,
        string? email,
        string? address)
    {
        Validate(name, documentType, taxId, phone);

        Name = name.Trim();
        DocumentType = documentType;
        TaxId = DocumentValidation.Normalize(documentType, taxId);
        Phone = Normalize(phone);
        Email = Normalize(email);
        Address = Normalize(address);
    }

    /// <summary>
    /// Deactivates the customer. This is a soft delete: customers are never removed physically.
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reactivates a previously deactivated customer.</summary>
    public void Activate() => IsActive = true;

    // Validates the required identifying fields and the optional phone shared by Create and Update.
    // The name stays free-form because a customer may be a person or a company, and company legal
    // names legitimately contain digits and symbols.
    private static void Validate(string name, DocumentType documentType, string taxId, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name is required.");
        }

        if (!DocumentValidation.IsValid(documentType, taxId))
        {
            throw new DomainException("The identity document is not valid for the selected type.");
        }

        if (!FieldValidation.IsValidPhone(phone))
        {
            throw new DomainException("The phone number is not valid.");
        }
    }

    // Trims the value and turns blank strings into null.
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
