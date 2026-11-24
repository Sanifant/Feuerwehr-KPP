namespace de.openelp.feuerwehr.Server.Services.InventoryModules
{
    using de.openelp.feuerwehr.Server.Data;
    using Microsoft.EntityFrameworkCore;
    using Npgsql;

    /// <summary>Periodically generates reminder digests; delivery is handled by the shared mail worker.</summary>
    public sealed class InventoryReminderWorker(IServiceScopeFactory scopes, TimeProvider clock,
        ILogger<InventoryReminderWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                    FeuerwehrDbContext db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
                    // Eine dedizierte PostgreSQL-Verbindung hält die Sperre auch über mehrere SaveChanges hinweg.
                    // API instances must not queue the same digest concurrently.
                    await using NpgsqlConnection mutex = new NpgsqlConnection(db.Database.GetConnectionString());
                    await mutex.OpenAsync(stoppingToken);
                    await using NpgsqlCommand command = new NpgsqlCommand("SELECT pg_try_advisory_lock(305002014)", mutex);
                    if (await command.ExecuteScalarAsync(stoppingToken) is true)
                    {
                        try
                        { await scope.ServiceProvider.GetRequiredService<IInventoryReminderService>().SendDueReminders(stoppingToken); }
                        finally
                        {
                            // Nicht an den bereits abgebrochenen Worker-Token binden. Auch Dispose schließt die Verbindung.
                            await using NpgsqlCommand release = new NpgsqlCommand("SELECT pg_advisory_unlock(305002014)", mutex) { CommandTimeout = 5 };
                            await release.ExecuteNonQueryAsync(CancellationToken.None);
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { logger.LogError(ex, "Fehler beim Einreihen der Inventarerinnerungen."); }

                try
                { await Task.Delay(TimeSpan.FromHours(6), clock, stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }
    }
}
