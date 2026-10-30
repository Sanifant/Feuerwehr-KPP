using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules;

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
    public bool IsVehicle { get; set; }
}

/// <summary>
/// Executes map Symbol Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
/// <param name="Category">The category.</param>
/// <param name="SvgContent">The svg Content.</param>
/// <param name="IsActive">The is Active.</param>
/// <param name="IsVehicle">The is Vehicle.</param>
public sealed record MapSymbolDto(Guid Id, string Name, string Category, string SvgContent, bool IsActive, bool IsVehicle = false);

public sealed class CreateMapSymbolCategoryRequest
{
    [Required, MaxLength(40)]
    public string Name { get; set; } = string.Empty;
}

