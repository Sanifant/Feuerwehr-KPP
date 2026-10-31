using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules;

public class Incident
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? IncidentNumber { get; set; }

    [MaxLength(200)]
    public string? Keyword { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    public double OriginLongitude { get; set; }

    public double OriginLatitude { get; set; }

    public IncidentStatus Status { get; set; } = IncidentStatus.Active;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime AlarmedAtUtc { get; set; }

    public DateTime? ClosedAtUtc { get; set; }

    [MaxLength(450)]
    public string? ClosedByUserId { get; set; }

    [MaxLength(200)]
    public string? ClosedByDisplayName { get; set; }

    public long Revision { get; set; }
}
