using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.IncidentModules;

public static class MapSymbolCategories
{
    public static readonly IReadOnlyList<string> All = Array.AsReadOnly(new[] { "Feuerwehr", "Rettungsdienst", "Personen", "Objekte" });
}

public sealed class SaveMapSymbolRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Category { get; set; } = string.Empty;

    [Required, MaxLength(262144)]
    public string SvgContent { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public sealed record MapSymbolDto(Guid Id, string Name, string Category, string SvgContent, bool IsActive);
