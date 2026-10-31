using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules;

public class MapElement
{
    [Key]
    public Guid Id { get; set; }

    public Guid IncidentId { get; set; }

    [ForeignKey(nameof(IncidentId))]
    public Incident Incident { get; set; } = null!;

    public MapElementType ElementType { get; set; }

    [MaxLength(120)]
    public string? SymbolId { get; set; }

    public string? SymbolSvg { get; set; }
    public bool IsVehicle { get; set; }

    [MaxLength(120)]
    public string? Category { get; set; }

    [MaxLength(200)]
    public string? Label { get; set; }

    [MaxLength(120)]
    public string? RadioCallName { get; set; }

    [MaxLength(120)]
    public string? Strength { get; set; }

    [MaxLength(2000)]
    public string? Note { get; set; }

    [MaxLength(20)]
    public string ColorHex { get; set; } = "#d62828";

    [Required]
    public string GeometryJson { get; set; } = string.Empty;

    public int Version { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    [MaxLength(450)]
    public string UpdatedByUserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string UpdatedByDisplayName { get; set; } = string.Empty;
}
