namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Carries vehicle data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the vehicle.</param>
    /// <param name="FireDepartmentId">The identifier of the associated fire department.</param>
    /// <param name="FireDepartmentName">The associated fire department's display name.</param>
    /// <param name="RadioCallSign">The vehicle's radio call sign.</param>
    /// <param name="Name">The display name of the vehicle.</param>
    /// <param name="LicensePlate">The vehicle registration plate.</param>
    /// <param name="EquipmentOfficerId">The identifier of the assigned equipment officer.</param>
    /// <param name="EquipmentOfficerName">The display name of the assigned equipment officer.</param>
    /// <param name="IsActive">The vehicle is active.</param>
    /// <param name="Version">The concurrency version used to detect conflicting updates.</param>
    /// <param name="CanEdit">The current user can edit the selected incident data.</param>
    /// <param name="CanBookDirectly">The current user can create confirmed vehicle bookings directly.</param>
    /// <param name="VehicleTypeId">The identifier of the vehicle type.</param>
    /// <param name="VehicleTypeName">The display name of the vehicle type.</param>
    /// <param name="VehicleStatusId">The identifier of the vehicle's configured status.</param>
    /// <param name="VehicleStatusName">The display name of the vehicle's configured status.</param>
    /// <param name="AllowsBooking">Vehicles with this status can be reserved.</param>
    /// <param name="StatusChangedAtUtc">The UTC timestamp of status changed for this vehicle.</param>
    public sealed record VehicleDto(
        Guid Id,
        int FireDepartmentId,
        string FireDepartmentName,
        string RadioCallSign,
        string? Name,
        string? LicensePlate,
        string EquipmentOfficerId,
        string EquipmentOfficerName,
        bool IsActive,
        int Version,
        bool CanEdit,
        bool CanBookDirectly,
        Guid? VehicleTypeId = null,
        string? VehicleTypeName = null,
        Guid VehicleStatusId = default,
        string VehicleStatusName = "Betriebsbereit",
        bool AllowsBooking = true,
        DateTime? StatusChangedAtUtc = null);
}
