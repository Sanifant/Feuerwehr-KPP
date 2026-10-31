using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules;

public class DiaryEntry
{
    [Key]
    public Guid Id { get; set; }

    public Guid IncidentId { get; set; }

    [ForeignKey(nameof(IncidentId))]
    public Incident Incident { get; set; } = null!;

    public long EntryNumber { get; set; }

    public int CurrentRevision { get; set; }

    public bool IsCanceled { get; set; }

    [MaxLength(1000)]
    public string? CancellationReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
