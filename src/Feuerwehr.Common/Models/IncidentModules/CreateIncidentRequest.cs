using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules;

public class CreateIncidentRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? IncidentNumber { get; set; }

    [MaxLength(200)]
    public string? Keyword { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Required]
    public DateTimeOffset? AlarmedAtUtc { get; set; }

    public double OriginLongitude { get; set; }

    public double OriginLatitude { get; set; }

    [Required]
    [MaxLength(120)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CommandId { get; set; } = string.Empty;
}
