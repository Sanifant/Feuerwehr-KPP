namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Carries vehicle availability data across the API boundary.
    /// </summary>
    /// <param name="Vehicle">The vehicle associated with this vehicle availability.</param>
    /// <param name="Days">The days associated with this vehicle availability.</param>
    public sealed record VehicleAvailabilityDto(VehicleDto Vehicle, IReadOnlyList<VehicleDayDto> Days);
}
