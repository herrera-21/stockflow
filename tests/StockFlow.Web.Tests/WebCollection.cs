namespace StockFlow.Web.Tests;

/// <summary>
/// xUnit collection that shares a single <see cref="StockFlowWebFactory"/> across all web tests, so
/// the database is migrated once instead of per test class.
/// </summary>
[CollectionDefinition(Name)]
public class WebCollection : ICollectionFixture<StockFlowWebFactory>
{
    /// <summary>Collection name referenced by <c>[Collection(WebCollection.Name)]</c>.</summary>
    public const string Name = "web";
}
