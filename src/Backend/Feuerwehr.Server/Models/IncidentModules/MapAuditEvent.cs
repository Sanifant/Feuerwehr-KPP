using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    /// <summary>
    /// Carries a live map audit notification and the associated incident revision.
    /// </summary>
    public class MapAuditEvent
    {
        /// <summary>
        /// Gets or sets the unique identifier of the map audit event.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the incident.
        /// </summary>
        public Guid IncidentId { get; set; }

        /// <summary>
        /// Gets or sets the sequence number allocated within the category.
        /// </summary>
        public long SequenceNumber { get; set; }

        /// <summary>
        /// Gets or sets the client-generated command identifier used to detect retries.
        /// </summary>
        [MaxLength(200)]
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the map element.
        /// </summary>
        public Guid? ElementId { get; set; }

        /// <summary>
        /// Gets or sets the operation name recorded in the audit or live event.
        /// </summary>
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the user account.
        /// </summary>
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the acting user's display name.
        /// </summary>
        [MaxLength(200)]
        public string UserDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC time at which the audited change occurred.
        /// </summary>
        public DateTime OccurredAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the serialized map-element state before the change.
        /// </summary>
        [Required]
        public string BeforeStateJson { get; set; } = "null";

        /// <summary>
        /// Gets or sets the serialized map-element state after the change.
        /// </summary>
        [Required]
        public string AfterStateJson { get; set; } = "null";
    }
}
