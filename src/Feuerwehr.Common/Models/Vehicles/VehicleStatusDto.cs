namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Carries vehicle status data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the vehicle status.</param>
    /// <param name="Name">The display name of the vehicle status.</param>
    /// <param name="AllowsBooking">Vehicles with this status can be reserved.</param>
    /// <param name="IsActive">The vehicle status is active.</param>
    /// <param name="Version">The concurrency version used to detect conflicting updates.</param>
    /// <param name="IsDefault">The status is the default for new vehicles.</param>
    public sealed record VehicleStatusDto(Guid Id, string Name, bool AllowsBooking, bool IsActive, int Version, bool IsDefault);
}
