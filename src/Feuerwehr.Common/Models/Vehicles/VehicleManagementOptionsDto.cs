namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Carries vehicle management options data across the API boundary.
    /// </summary>
    /// <param name="Departments">The departments associated with this vehicle management options.</param>
    /// <param name="Officers">The officers associated with this vehicle management options.</param>
    /// <param name="DefaultStatusId">The identifier of the default status for this vehicle type.</param>
    public sealed record VehicleManagementOptionsDto(IReadOnlyList<VehicleDepartmentDto> Departments, IReadOnlyList<VehicleOfficerDto> Officers, Guid DefaultStatusId = default);
}
