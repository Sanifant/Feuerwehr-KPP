using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
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
}
