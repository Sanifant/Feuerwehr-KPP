using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    public class DiaryEntryRevision
    {
        [Key]
        public Guid Id { get; set; }

        public Guid DiaryEntryId { get; set; }

        [ForeignKey(nameof(DiaryEntryId))]
        public DiaryEntry DiaryEntry { get; set; } = null!;

        public int RevisionNumber { get; set; }

        public DiaryRevisionType RevisionType { get; set; }

        public DateTime EventTimestampUtc { get; set; }

        [MaxLength(120)]
        public string CategoryCode { get; set; } = string.Empty;

        [MaxLength(120)]
        public string CategoryNameSnapshot { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string Text { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Sender { get; set; }

        [MaxLength(200)]
        public string? Recipient { get; set; }

        [MaxLength(120)]
        public string? TransmissionType { get; set; }

        [MaxLength(1000)]
        public string? CancellationReason { get; set; }

        [MaxLength(450)]
        public string CreatedByUserId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string CreatedByDisplayName { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }
}
