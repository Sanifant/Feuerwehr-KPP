namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Carries vehicle day data across the API boundary.
    /// </summary>
    /// <param name="Date">The calendar date represented by this record.</param>
    /// <param name="Status">The current state of the vehicle day.</param>
    public sealed record VehicleDayDto(DateOnly Date, VehicleDayStatus Status);
}
