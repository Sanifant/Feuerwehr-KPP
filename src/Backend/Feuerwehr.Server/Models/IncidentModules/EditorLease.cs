using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    /// <summary>
    /// Stores the session, owner, token, and expiry of a situation-map editing lease.
    /// </summary>
    public class EditorLease
    {
        /// <summary>
        /// Gets or sets the unique identifier of the editor lease.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the incident.
        /// </summary>
        public Guid IncidentId { get; set; }

        /// <summary>
        /// Gets or sets the incident associated with this editor lease.
        /// </summary>
        [ForeignKey(nameof(IncidentId))]
        public Incident Incident { get; set; } = null!;

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
        /// Gets or sets the client session that owns or requests the editing lease.
        /// </summary>
        [MaxLength(120)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the opaque token proving ownership of the editing lease.
        /// </summary>
        [MaxLength(120)]
        public string LeaseToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC expiry time of the invitation or lease.
        /// </summary>
        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC time of the most recent editor heartbeat.
        /// </summary>
        public DateTime LastHeartbeatAtUtc { get; set; }
    }
}
