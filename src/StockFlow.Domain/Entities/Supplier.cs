using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

/// <summary>
/// Supplier aggregate: business information, tax identification and contact details. Suppliers are
/// never deleted physically; they are deactivated instead.
/// </summary>
public class Supplier
{
    // Products offered by this supplier (many-to-many through ProductSupplier).
    private readonly List<ProductSupplier> _products = [];

    // EF Core materialization constructor.
    private Supplier()
    {
    }

    private Supplier(
        Guid id,
        string name,
        string taxId,
        string? contactName,
        string? phone,
        string? email,
        string? address,
        bool isActive)
    {
        Id = id;
        Name = name;
        TaxId = taxId;
        ContactName = contactName;
        Phone = phone;
        Email = email;
        Address = address;
        IsActive = isActive;
    }

    /// <summary>Unique identifier (UUID v7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Supplier company name.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Tax identification; unique across suppliers.</summary>
    public string TaxId { get; private set; } = null!;

    /// <summary>Optional name of the person to contact at the supplier.</summary>
    public string? ContactName { get; private set; }

    /// <summary>Optional contact phone number.</summary>
    public string? Phone { get; private set; }

    /// <summary>Optional contact email address.</summary>
    public string? Email { get; private set; }

    /// <summary>Optional postal address.</summary>
    public string? Address { get; private set; }

    /// <summary>Whether the supplier is active (soft-delete flag).</summary>
    public bool IsActive { get; private set; }

    /// <summary>Products offered by this supplier.</summary>
    public IReadOnlyList<ProductSupplier> Products => _products;

    /// <summary>
    /// Creates a new active supplier.
    /// </summary>
    /// <param name="name">Company name; must not be blank.</param>
    /// <param name="taxId">Tax identification; must not be blank.</param>
    /// <param name="contactName">Optional contact person name.</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional postal address.</param>
    /// <returns>The created supplier.</returns>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public static Supplier Create(
        string name,
        string taxId,
        string? contactName,
        string? phone,
        string? email,
        string? address)
    {
        Validate(name, taxId);

        return new Supplier(
            Guid.CreateVersion7(),
            name.Trim(),
            taxId.Trim(),
            Normalize(contactName),
            Normalize(phone),
            Normalize(email),
            Normalize(address),
            isActive: true);
    }

    /// <summary>
    /// Updates the supplier's editable data.
    /// </summary>
    /// <param name="name">Company name; must not be blank.</param>
    /// <param name="taxId">Tax identification; must not be blank.</param>
    /// <param name="contactName">Optional contact person name.</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional postal address.</param>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public void Update(
        string name,
        string taxId,
        string? contactName,
        string? phone,
        string? email,
        string? address)
    {
        Validate(name, taxId);

        Name = name.Trim();
        TaxId = taxId.Trim();
        ContactName = Normalize(contactName);
        Phone = Normalize(phone);
        Email = Normalize(email);
        Address = Normalize(address);
    }

    /// <summary>
    /// Deactivates the supplier. This is a soft delete: suppliers are never removed physically.
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reactivates a previously deactivated supplier.</summary>
    public void Activate() => IsActive = true;

    // Validates the required identifying fields shared by Create and Update.
    private static void Validate(string name, string taxId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(taxId))
        {
            throw new DomainException("Tax identification is required.");
        }
    }

    // Trims the value and turns blank strings into null.
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
