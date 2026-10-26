using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Server.Models.IncidentModules;

public sealed class MapSymbol
{
    public Guid Id { get; set; }
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(40)]
    public string Category { get; set; } = string.Empty;
    [Required]
    public string SvgContent { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsVehicle { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class MapSymbolCategory
{
    [Key, MaxLength(40)]
    public string Name { get; set; } = string.Empty;
}
