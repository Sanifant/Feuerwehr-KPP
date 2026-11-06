using System.Text.Json;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Executes outbox Service.
    /// </summary>
    /// <param name="dbContext">The db Context.</param>
    public sealed class OutboxService(FeuerwehrDbContext dbContext) : IOutboxService
    {
        /// <summary>
        /// Adds the message.
        /// </summary>
        /// <param name="aggregateType">The aggregate Type.</param>
        /// <param name="eventType">The event Type.</param>
        /// <param name="aggregateId">The aggregate Id.</param>
        /// <param name="payload">The payload.</param>
        public void AddMessage(string aggregateType, string eventType, string aggregateId, object payload)
        {
            _ = dbContext.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateType = aggregateType,
                EventType = eventType,
                AggregateId = aggregateId,
                PayloadJson = JsonSerializer.Serialize(payload, JsonOptionsFactory.Default),
                CreatedAtUtc = DateTime.UtcNow,
            });
        }
    }
}
