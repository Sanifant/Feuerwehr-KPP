using System.ComponentModel.DataAnnotations;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    /// <summary>
    /// Stores an incident's reference, lifecycle state, revision, and closure information.
    /// </summary>
    public class Incident
    {
        /// <summary>
        /// Gets or sets the unique identifier of the incident.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the incident.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the incident's external reference number.
        /// </summary>
        [MaxLength(100)]
        public string? IncidentNumber { get; set; }

        /// <summary>
        /// Gets or sets the incident dispatch keyword.
        /// </summary>
        [MaxLength(200)]
        public string? Keyword { get; set; }

        /// <summary>
        /// Gets or sets the descriptive text for the incident.
        /// </summary>
        [MaxLength(4000)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the map origin longitude in decimal degrees.
        /// </summary>
        public double OriginLongitude { get; set; }

        /// <summary>
        /// Gets or sets the map origin latitude in decimal degrees.
        /// </summary>
        public double OriginLatitude { get; set; }

        /// <summary>
        /// Gets or sets the current state of the incident.
        /// </summary>
        public IncidentStatus Status { get; set; } = IncidentStatus.Active;

        /// <summary>
        /// Gets or sets the UTC time at which the record was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC time at which the incident alarm was raised.
        /// </summary>
        public DateTime AlarmedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC time at which the incident was closed.
        /// </summary>
        public DateTime? ClosedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who closed the incident.
        /// </summary>
        [MaxLength(450)]
        public string? ClosedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the display name of the user who closed the incident.
        /// </summary>
        [MaxLength(200)]
        public string? ClosedByDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the revision counter for the incident.
        /// </summary>
        public long Revision { get; set; }
    }
}
