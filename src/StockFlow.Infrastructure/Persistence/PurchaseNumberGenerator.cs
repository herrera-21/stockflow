using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Domain;

namespace StockFlow.Infrastructure.Persistence;

/// <summary>
/// Reads the next value from the SQL Server purchase order sequence and formats it with the domain
/// rule (<see cref="PurchaseNumber"/>). A database sequence is atomic, so two concurrent requests
/// never get the same order number.
/// </summary>
public class PurchaseNumberGenerator : IPurchaseNumberGenerator
{
    // NEXT VALUE FOR is not allowed inside a subquery, so it is executed as a plain scalar command
    // instead of going through an EF Core composable query.
    private const string NextValueSql = "SELECT NEXT VALUE FOR [dbo].[PurchaseOrderNumberSequence]";

    // Context used to reach the underlying SQL Server connection.
    private readonly AppDbContext _db;

    /// <summary>Initializes the generator.</summary>
    /// <param name="db">Persistence context whose connection is used to read the sequence.</param>
    public PurchaseNumberGenerator(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Reads and formats the next purchase order number.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next unique order number, for example <c>OC-000001</c>.</returns>
    public async Task<string> NextNumberAsync(CancellationToken cancellationToken = default)
    {
        // Open/close are counter-based in EF Core, so this is safe even when the connection is
        // already open because of an ambient transaction.
        await _db.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = _db.Database.GetDbConnection().CreateCommand();
            command.CommandText = NextValueSql;

            var value = await command.ExecuteScalarAsync(cancellationToken);

            return PurchaseNumber.Format(Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }
}
