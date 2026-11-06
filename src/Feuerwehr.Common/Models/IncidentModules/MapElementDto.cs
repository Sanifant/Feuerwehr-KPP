namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Carries map element data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the map element.</param>
    /// <param name="IncidentId">The identifier of the incident.</param>
    /// <param name="ElementType">The geometry kind of the map element.</param>
    /// <param name="SymbolId">The identifier of the map symbol assigned to the element.</param>
    /// <param name="Category">The category label assigned to the map element.</param>
    /// <param name="Label">The label displayed for the map element.</param>
    /// <param name="RadioCallName">The radio call name displayed on the situation map.</param>
    /// <param name="Strength">The personnel strength associated with the map element.</param>
    /// <param name="Note">The note recorded for the map element.</param>
    /// <param name="ColorHex">The display color encoded as a hexadecimal color string.</param>
    /// <param name="GeometryJson">The GeoJSON geometry of the map element.</param>
    /// <param name="Version">The concurrency version used to detect conflicting updates.</param>
    /// <param name="UpdatedAtUtc">The UTC time of the latest change.</param>
    /// <param name="UpdatedByDisplayName">The last editor's display name captured with the record.</param>
    /// <param name="SymbolSvg">The SVG markup captured for rendering the map element.</param>
    /// <param name="IsVehicle">The map symbol represents a vehicle.</param>
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
