using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

/// <summary>
/// Customer aggregate: general data, tax identification, contact details and address. Customers are
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
        string taxId,
        string? phone,
        string? email,
        string? address,
        bool isActive)
    {
        Id = id;
        Name = name;
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

    /// <summary>Tax identification; unique across customers.</summary>
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
    /// <param name="name">Display name; must not be blank.</param>
    /// <param name="taxId">Tax identification; must not be blank.</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional postal address.</param>
    /// <returns>The created customer.</returns>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public static Customer Create(
        string name,
        string taxId,
        string? phone,
        string? email,
        string? address)
    {
        Validate(name, taxId);

        return new Customer(
            Guid.CreateVersion7(),
            name.Trim(),
            taxId.Trim(),
            Normalize(phone),
            Normalize(email),
            Normalize(address),
            isActive: true);
    }

    /// <summary>
    /// Updates the customer's editable data.
    /// </summary>
    /// <param name="name">Display name; must not be blank.</param>
    /// <param name="taxId">Tax identification; must not be blank.</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional postal address.</param>
    /// <exception cref="DomainException">When any invariant is violated.</exception>
    public void Update(
        string name,
        string taxId,
        string? phone,
        string? email,
        string? address)
    {
        Validate(name, taxId);

        Name = name.Trim();
        TaxId = taxId.Trim();
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
