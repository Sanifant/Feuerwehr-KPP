namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Describes a vehicle import validation failure and its optional source line.
    /// </summary>
    /// <param name="LineNumber">The source-file line associated with the error, or null for a file-level error.</param>
    /// <param name="Description">The descriptive text for the vehicle import error.</param>
    public sealed record VehicleImportError(long? LineNumber, string Description);
}
