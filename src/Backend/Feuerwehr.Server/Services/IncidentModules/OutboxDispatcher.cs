using System.Text.Json;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Hubs;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Executes outbox Dispatcher.
    /// </summary>
    /// <param name="serviceProvider">The service Provider.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="hubContext">The hub Context.</param>
    public sealed class OutboxDispatcher(
        IServiceProvider serviceProvider,
        ILogger<OutboxDispatcher> logger,
        IHubContext<IncidentHub, IIncidentClient> hubContext) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using IServiceScope scope = serviceProvider.CreateScope();
                    FeuerwehrDbContext db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();

                    List<OutboxMessage> batch = await db.OutboxMessages
                        .Where(x => x.ProcessedAtUtc == null)
                        .OrderBy(x => x.CreatedAtUtc)
                        .Take(50)
                        .ToListAsync(stoppingToken);

                    foreach (OutboxMessage? message in batch)
                    {
                        try
                        {
                            await this.PublishMessageAsync(message, stoppingToken);
                            message.ProcessedAtUtc = DateTime.UtcNow;
                            message.DeliveryAttempts += 1;
                            message.LastError = null;
                        }
                        catch (Exception ex)
                        {
                            message.DeliveryAttempts += 1;
                            message.LastError = ex.Message;
                            logger.LogWarning(ex, "Outbox dispatch failed for {OutboxId}", message.Id);
                        }
                    }

                    if (batch.Count > 0)
                    {
                        _ = await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Outbox dispatcher loop failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }

        private async Task PublishMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            using JsonDocument doc = JsonDocument.Parse(message.PayloadJson);
            JsonElement payload = doc.RootElement.Clone();

            switch (message.AggregateType)
            {
                case "incident":
                    await hubContext.Clients.Group($"incident:{message.AggregateId}").IncidentUpdated(payload);
                    break;
                case "map":
                    await hubContext.Clients.Group($"map:{message.AggregateId}").MapUpdated(payload);
                    break;
                case "diary":
                    await hubContext.Clients.Group($"diary:{message.AggregateId}").DiaryUpdated(payload);
                    break;
                default:
                    await hubContext.Clients.Group($"incident:{message.AggregateId}").IncidentUpdated(payload);
                    break;
            }

            _ = cancellationToken;
        }
    }
}
