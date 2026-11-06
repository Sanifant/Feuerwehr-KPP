using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Specifies the input for create incident operations.
    /// </summary>
    public class CreateIncidentRequest
    {
        /// <summary>
        /// Gets or sets the display name of the create incident.
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
        /// Gets or sets the descriptive text for the create incident.
        /// </summary>
        [MaxLength(4000)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the UTC time at which the incident alarm was raised.
        /// </summary>
        [Required]
        public DateTimeOffset? AlarmedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the map origin longitude in decimal degrees.
        /// </summary>
        public double OriginLongitude { get; set; }

        /// <summary>
        /// Gets or sets the map origin latitude in decimal degrees.
        /// </summary>
        public double OriginLatitude { get; set; }

        /// <summary>
        /// Gets or sets the client session that owns or requests the editing lease.
        /// </summary>
        [Required]
        [MaxLength(120)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the client-generated command identifier used to detect retries.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string CommandId { get; set; } = string.Empty;
    }
}
