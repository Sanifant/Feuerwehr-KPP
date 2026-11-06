using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    /// <summary>
    /// Stores an immutable diary revision with the recorded content and author identity.
    /// </summary>
    public class DiaryEntryRevision
    {
        /// <summary>
        /// Gets or sets the unique identifier of the diary entry revision.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the diary entry owning this revision.
        /// </summary>
        public Guid DiaryEntryId { get; set; }

        /// <summary>
        /// Gets or sets the diary entry owning this revision.
        /// </summary>
        [ForeignKey(nameof(DiaryEntryId))]
        public DiaryEntry DiaryEntry { get; set; } = null!;

        /// <summary>
        /// Gets or sets the sequential number of this diary revision.
        /// </summary>
        public int RevisionNumber { get; set; }

        /// <summary>
        /// Gets or sets the kind of diary revision: creation, update, or cancellation.
        /// </summary>
        public DiaryRevisionType RevisionType { get; set; }

        /// <summary>
        /// Gets or sets the UTC time of the event described by the diary entry.
        /// </summary>
        public DateTime EventTimestampUtc { get; set; }

        /// <summary>
        /// Gets or sets the category code used in equipment barcode allocation.
        /// </summary>
        [MaxLength(120)]
        public string CategoryCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the category name captured when the inspection was recorded.
        /// </summary>
        [MaxLength(120)]
        public string CategoryNameSnapshot { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the text content of the diary entry revision.
        /// </summary>
        [MaxLength(4000)]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the configured sender email address.
        /// </summary>
        [MaxLength(200)]
        public string? Sender { get; set; }

        /// <summary>
        /// Gets or sets the destination email address.
        /// </summary>
        [MaxLength(200)]
        public string? Recipient { get; set; }

        /// <summary>
        /// Gets or sets the transmission channel recorded in the diary entry.
        /// </summary>
        [MaxLength(120)]
        public string? TransmissionType { get; set; }

        /// <summary>
        /// Gets or sets the reason recorded when canceling the diary entry.
        /// </summary>
        [MaxLength(1000)]
        public string? CancellationReason { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who created the record.
        /// </summary>
        [MaxLength(450)]
        public string CreatedByUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creator's display name captured with the record.
        /// </summary>
        [MaxLength(200)]
        public string CreatedByDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC time at which the record was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }
    }
}
