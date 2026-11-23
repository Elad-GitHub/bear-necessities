using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BearNecessities.Infrastructure.Persistence;

/// <summary>
/// Migrates and seeds the database at startup, waiting for SQL Server first: after a reboot Docker starts
/// the SQL container a little after the API, and the API used to crash on its first connection.
/// </summary>
public static class DatabaseStartup
{
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    public static async Task MigrateAndSeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseStartup));
        var giveUpAt = DateTime.UtcNow + MaxWait;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BearNecessitiesDbContext>();
                await dbContext.Database.MigrateAsync(cancellationToken);
                await DemoDataSeeder.SeedDemoDataAsync(dbContext);
                return;
            }
            catch (Exception ex) when (IsDatabaseUnavailable(ex) && DateTime.UtcNow + RetryDelay < giveUpAt)
            {
                logger.LogWarning("Database not reachable yet (attempt {Attempt}): {Message}. Retrying in {Delay}s",
                    attempt, (ex.InnerException ?? ex).Message, RetryDelay.TotalSeconds);
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }

    // While SQL Server is still starting, EF wraps the SqlException in an InvalidOperationException ("transient failure")
    private static bool IsDatabaseUnavailable(Exception ex) =>
        ex is SqlException || ex is InvalidOperationException { InnerException: SqlException };
}
