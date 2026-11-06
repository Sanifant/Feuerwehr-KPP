using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    /// <summary>
    /// Stores a committed incident event awaiting delivery to live clients.
    /// </summary>
    public class OutboxMessage
    {
        /// <summary>
        /// Gets or sets the unique identifier of the outbox message.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the aggregate type that produced the event.
        /// </summary>
        [MaxLength(120)]
        public string AggregateType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the event name used to dispatch the live notification.
        /// </summary>
        [MaxLength(120)]
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the aggregate that produced the event.
        /// </summary>
        [MaxLength(200)]
        public string AggregateId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the serialized event payload.
        /// </summary>
        [Required]
        public string PayloadJson { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC time at which the record was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC time of successful outbox dispatch, or null while pending.
        /// </summary>
        public DateTime? ProcessedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the number of notification delivery attempts.
        /// </summary>
        public int DeliveryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the error recorded by the most recent failed delivery attempt.
        /// </summary>
        [MaxLength(2000)]
        public string? LastError { get; set; }
    }
}
