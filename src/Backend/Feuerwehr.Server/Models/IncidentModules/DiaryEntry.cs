using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    /// <summary>
    /// Stores a numbered incident diary entry and its current immutable revision.
    /// </summary>
    public class DiaryEntry
    {
        /// <summary>
        /// Gets or sets the unique identifier of the diary entry.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the incident.
        /// </summary>
        public Guid IncidentId { get; set; }

        /// <summary>
        /// Gets or sets the incident associated with this diary entry.
        /// </summary>
        [ForeignKey(nameof(IncidentId))]
        public Incident Incident { get; set; } = null!;

        /// <summary>
        /// Gets or sets the sequential entry number within the incident diary.
        /// </summary>
        public long EntryNumber { get; set; }

        /// <summary>
        /// Gets or sets the latest revision number of the diary entry.
        /// </summary>
        public int CurrentRevision { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the diary entry has been canceled while preserving its revision history.
        /// </summary>
        public bool IsCanceled { get; set; }

        /// <summary>
        /// Gets or sets the reason recorded when canceling the diary entry.
        /// </summary>
        [MaxLength(1000)]
        public string? CancellationReason { get; set; }

        /// <summary>
        /// Gets or sets the UTC time at which the record was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC time of the latest change.
        /// </summary>
        public DateTime UpdatedAtUtc { get; set; }
    }
}
