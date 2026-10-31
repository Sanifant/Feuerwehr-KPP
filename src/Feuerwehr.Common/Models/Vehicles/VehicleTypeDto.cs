using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Executes vehicle Type Dto.
    /// </summary>
    /// <param name="Id">The id.</param>
    /// <param name="Name">The name.</param>
    /// <param name="IsActive">The is Active.</param>
    /// <param name="Version">The version.</param>
    public sealed record VehicleTypeDto(Guid Id, string Name, bool IsActive, int Version);
}
