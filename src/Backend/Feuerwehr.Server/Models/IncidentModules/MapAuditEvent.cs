using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules;

public class MapAuditEvent
{
    [Key]
    public Guid Id { get; set; }

    public Guid IncidentId { get; set; }

    public long SequenceNumber { get; set; }

    [MaxLength(200)]
    public string CommandId { get; set; } = string.Empty;

    public Guid? ElementId { get; set; }

    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string UserDisplayName { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    [Required]
    public string BeforeStateJson { get; set; } = "null";

    [Required]
    public string AfterStateJson { get; set; } = "null";
}
