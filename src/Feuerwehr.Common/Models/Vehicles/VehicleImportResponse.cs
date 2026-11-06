namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Carries the result of vehicle import operations.
    /// </summary>
    /// <param name="CreatedCount">The number of records created by the import.</param>
    /// <param name="UpdatedCount">The number of existing records updated by the import.</param>
    /// <param name="Errors">The validation errors found while importing the file.</param>
    public sealed record VehicleImportResponse(int CreatedCount, int UpdatedCount, IReadOnlyList<VehicleImportError> Errors);
}
