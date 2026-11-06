namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Carries vehicle officer data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the vehicle officer.</param>
    /// <param name="Name">The display name of the vehicle officer.</param>
    /// <param name="FireDepartmentId">The identifier of the associated fire department.</param>
    public sealed record VehicleOfficerDto(string Id, string Name, int FireDepartmentId);
}
