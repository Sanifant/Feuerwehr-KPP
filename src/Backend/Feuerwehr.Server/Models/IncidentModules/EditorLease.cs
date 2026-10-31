using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules;

public class EditorLease
{
    [Key]
    public Guid Id { get; set; }

    public Guid IncidentId { get; set; }

    [ForeignKey(nameof(IncidentId))]
    public Incident Incident { get; set; } = null!;

    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string UserDisplayName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string SessionId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string LeaseToken { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime LastHeartbeatAtUtc { get; set; }
}
