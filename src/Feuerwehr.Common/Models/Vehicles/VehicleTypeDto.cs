namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Carries vehicle type data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the vehicle type.</param>
    /// <param name="Name">The display name of the vehicle type.</param>
    /// <param name="IsActive">The vehicle type is active.</param>
    /// <param name="Version">The concurrency version used to detect conflicting updates.</param>
    public sealed record VehicleTypeDto(Guid Id, string Name, bool IsActive, int Version);
}
