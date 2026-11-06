using De.Openelp.Feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Executes inventory Reminder Worker.
    /// </summary>
    /// <param name="scopes">The scopes.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="logger">The logger.</param>
    public sealed class InventoryReminderWorker(
        IServiceScopeFactory scopes,
        TimeProvider clock,
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
                    await using NpgsqlConnection mutex = new(db.Database.GetConnectionString());
                    await mutex.OpenAsync(stoppingToken);
                    await using NpgsqlCommand command = new("SELECT pg_try_advisory_lock(305002014)", mutex);
                    if (await command.ExecuteScalarAsync(stoppingToken) is true)
                    {
                        try
                        {
                            await scope.ServiceProvider.GetRequiredService<IInventoryReminderService>().SendDueReminders(stoppingToken);
                        }
                        finally
                        {
                            await using NpgsqlCommand release = new("SELECT pg_advisory_unlock(305002014)", mutex) { CommandTimeout = 5 };
                            _ = await release.ExecuteNonQueryAsync(CancellationToken.None);
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Fehler beim Versand der Inventarerinnerungen.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromHours(6), clock, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
