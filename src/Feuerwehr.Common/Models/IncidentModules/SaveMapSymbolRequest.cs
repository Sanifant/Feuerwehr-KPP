using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules;

public sealed class SaveMapSymbolRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Category { get; set; } = string.Empty;

    [Required, MaxLength(262144)]
    public string SvgContent { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public bool IsVehicle { get; set; }
}
