using Npgsql;
using ReadingLibrary.Sync;

namespace ReadingLibrary.API.Sync;

public class SyncWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SyncWorker> logger) : BackgroundService
{
    private readonly TimeSpan _interval =
        TimeSpan.FromMinutes(configuration.GetValue("Sync:IntervalMinutes", 60));

    private const long SyncAdvisoryLockId = 7483920156L;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (true)
        {
            try
            {
                logger.LogInformation("Starting books/authors sync");
                await SyncAsync(stoppingToken);
                logger.LogInformation("Sync completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sync failed");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        await using var lockConn = new NpgsqlConnection(connectionString);
        await lockConn.OpenAsync(ct);

        await using var lockCmd = new NpgsqlCommand("SELECT pg_try_advisory_lock(@id)", lockConn);
        lockCmd.Parameters.AddWithValue("id", SyncAdvisoryLockId);

        var acquired = (bool)(await lockCmd.ExecuteScalarAsync(ct))!;
        if (!acquired)
        {
            logger.LogInformation("Sync skipped — another instance is already syncing");
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<LibrarySyncer>().SyncAsync(ct);
        }
        finally
        {
            await using var unlockCmd = new NpgsqlCommand("SELECT pg_advisory_unlock(@id)", lockConn);
            unlockCmd.Parameters.AddWithValue("id", SyncAdvisoryLockId);
            await unlockCmd.ExecuteNonQueryAsync(ct);
        }
    }
}
