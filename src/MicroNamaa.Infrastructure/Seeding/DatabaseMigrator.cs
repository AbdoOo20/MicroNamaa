using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Options;

using MicroNamaa.Application.Settings;

using MicroNamaa.Infrastructure.Persistence;

namespace MicroNamaa.Infrastructure.Seeding;

public static class DatabaseMigrator
{
    /// <summary>Applies pending migrations when <c>Database:MigrateOnStartup</c> is true.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        if (!scope.ServiceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value.MigrateOnStartup)
        {
            return;
        }

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigrator");

        try
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(cancellationToken);
        }

        catch (Exception exception)
        {
            logger.LogError(exception, "The database could not be migrated. Check the connection string.");
        }
    }
}
