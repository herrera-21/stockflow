using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StockFlow.Infrastructure.Persistence;

/// <summary>
/// Applies the pending EF Core migrations at application startup so the schema is always current.
/// </summary>
public static class DatabaseInitializer
{
    // The SQL Server container may still be booting when the app starts, so connection failures are
    // retried a few times before giving up.
    private const int MaxAttempts = 10;

    /// <summary>Delay between connection or migration attempts.</summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Applies every pending migration, creating the database when it does not exist yet.
    /// Safe to run on each startup: already-applied migrations are skipped.
    /// </summary>
    /// <param name="services">The scoped service provider used to resolve the database context.</param>
    /// <param name="cancellationToken">Token used to cancel the wait and the migration.</param>
    /// <returns>A task that completes once the schema is up to date.</returns>
    /// <remarks>The last attempt lets any exception surface so startup fails loudly.</remarks>
    public static async Task MigrateAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<AppDbContext>>();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await context.Database.MigrateAsync(cancellationToken);
                return;
            }
            catch (Exception exception) when (attempt < MaxAttempts)
            {
                logger.LogWarning(
                    exception,
                    "Database migration attempt {Attempt} of {MaxAttempts} failed; retrying in {Delay} seconds.",
                    attempt,
                    MaxAttempts,
                    RetryDelay.TotalSeconds);

                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }
}
