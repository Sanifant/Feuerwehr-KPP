namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Carries map symbol data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the map symbol.</param>
    /// <param name="Name">The display name of the map symbol.</param>
    /// <param name="Category">The category label assigned to the map symbol.</param>
    /// <param name="SvgContent">The validated SVG markup of the map symbol.</param>
    /// <param name="IsActive">The map symbol is active.</param>
    /// <param name="IsVehicle">The map symbol represents a vehicle.</param>
    public sealed record MapSymbolDto(Guid Id, string Name, string Category, string SvgContent, bool IsActive, bool IsVehicle = false);
}
