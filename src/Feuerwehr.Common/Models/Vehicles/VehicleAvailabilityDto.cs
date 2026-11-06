using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Executes vehicle Availability Dto.
    /// </summary>
    /// <param name="Vehicle">The vehicle.</param>
    /// <param name="Days">The days.</param>
    public sealed record VehicleAvailabilityDto(VehicleDto Vehicle, IReadOnlyList<VehicleDayDto> Days);
}
