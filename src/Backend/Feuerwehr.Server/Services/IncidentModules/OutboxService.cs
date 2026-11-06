using System.Text.Json;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Adds live-update events to the current database unit of work for later dispatch.
    /// </summary>
    /// <param name="dbContext">The database context for the current unit of work.</param>
    public sealed class OutboxService(FeuerwehrDbContext dbContext) : IOutboxService
    {
        /// <summary>
        /// Stages an outbox message in the current database unit of work for delivery after commit.
        /// </summary>
        /// <param name="aggregateType">The aggregate type that produced the event.</param>
        /// <param name="eventType">The event name used to dispatch the live notification.</param>
        /// <param name="aggregateId">The identifier of the aggregate that produced the event.</param>
        /// <param name="payload">The command or event data to serialize.</param>
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
