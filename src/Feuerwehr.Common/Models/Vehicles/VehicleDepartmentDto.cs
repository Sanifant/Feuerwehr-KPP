namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Carries vehicle department data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the vehicle department.</param>
    /// <param name="Name">The display name of the vehicle department.</param>
    public sealed record VehicleDepartmentDto(int Id, string Name);
}
