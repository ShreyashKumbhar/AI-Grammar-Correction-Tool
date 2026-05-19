using Microsoft.EntityFrameworkCore;

namespace GrammarCorrector.Data;

/// <summary>
/// Ensures the application database schema exists before handling requests.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Applying {Count} pending migration(s): {Names}",
                pending.Count, string.Join(", ", pending));
            await db.Database.MigrateAsync(cancellationToken);
            return;
        }

        if (await UsersTableExistsAsync(db, cancellationToken))
        {
            await SyncSubscriptionPricingAsync(db, cancellationToken);
            return;
        }

        logger.LogWarning(
            "Database is connected but application tables are missing. Creating schema from the current model.");

        // Database exists (e.g. with __EFMigrationsHistory) but has no app tables.
        await db.Database.EnsureDeletedAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);

        logger.LogInformation("Database schema created successfully.");
        await SyncSubscriptionPricingAsync(db, cancellationToken);
    }

    private static async Task SyncSubscriptionPricingAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE [Subscriptions]
            SET [MonthlyPrice] = 99,
                [Description] = N'Unlimited corrections with priority support (₹99/month)'
            WHERE [Tier] = 1
            """,
            cancellationToken);
    }

    private static async Task<bool> UsersTableExistsAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT OBJECT_ID(N'[Users]', N'U')";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null and not DBNull;
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }
}
