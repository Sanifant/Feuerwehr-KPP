namespace de.openelp.feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Executes vehicle Day Dto.
    /// </summary>
    /// <param name="Date">The date.</param>
    /// <param name="Status">The status.</param>
    public sealed record VehicleDayDto(DateOnly Date, VehicleDayStatus Status);
}
