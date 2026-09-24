namespace StockFlow.Domain;

/// <summary>
/// Fixed identifiers for the seeded categories. They are constants so the seed data, the data
/// migration and the tests all reference the same rows.
/// </summary>
public static class CategoryIds
{
    /// <summary>Cleaning supplies.</summary>
    public static readonly Guid Cleaning = Guid.Parse("0a000000-0000-0000-0000-000000000001");

    /// <summary>Beverages.</summary>
    public static readonly Guid Beverages = Guid.Parse("0a000000-0000-0000-0000-000000000002");

    /// <summary>Food.</summary>
    public static readonly Guid Food = Guid.Parse("0a000000-0000-0000-0000-000000000003");

    /// <summary>Snacks.</summary>
    public static readonly Guid Snacks = Guid.Parse("0a000000-0000-0000-0000-000000000004");

    /// <summary>Dairy products.</summary>
    public static readonly Guid Dairy = Guid.Parse("0a000000-0000-0000-0000-000000000005");

    /// <summary>Bakery products.</summary>
    public static readonly Guid Bakery = Guid.Parse("0a000000-0000-0000-0000-000000000006");

    /// <summary>Personal care products.</summary>
    public static readonly Guid PersonalCare = Guid.Parse("0a000000-0000-0000-0000-000000000007");

    /// <summary>Fallback category for products without a specific one.</summary>
    public static readonly Guid Other = Guid.Parse("0a000000-0000-0000-0000-000000000008");
}
