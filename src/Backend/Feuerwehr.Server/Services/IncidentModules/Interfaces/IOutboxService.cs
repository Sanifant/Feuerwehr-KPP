namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Defines the outbox operations required by application consumers.
    /// </summary>
    public interface IOutboxService
    {
        /// <summary>
        /// Stages an outbox message in the current database unit of work for delivery after commit.
        /// </summary>
        /// <param name="aggregateType">The aggregate type that produced the event.</param>
        /// <param name="eventType">The event name used to dispatch the live notification.</param>
        /// <param name="aggregateId">The identifier of the aggregate that produced the event.</param>
        /// <param name="payload">The command or event data to serialize.</param>
        void AddMessage(string aggregateType, string eventType, string aggregateId, object payload);
    }
}
