using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    public record MapElementDto(
        Guid Id,
        Guid IncidentId,
        MapElementType ElementType,
        string? SymbolId,
        string? Category,
        string? Label,
        string? RadioCallName,
        string? Strength,
        string? Note,
        string ColorHex,
        string GeometryJson,
        int Version,
        DateTime UpdatedAtUtc,
        string UpdatedByDisplayName,
        string? SymbolSvg = null,
        bool IsVehicle = false);
}
