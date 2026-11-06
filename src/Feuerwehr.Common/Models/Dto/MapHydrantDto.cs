namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries map hydrant data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the map hydrant.</param>
    /// <param name="Longitude">The longitude in decimal degrees.</param>
    /// <param name="Latitude">The latitude in decimal degrees.</param>
    /// <param name="Status">The current state of the map hydrant.</param>
    /// <param name="NominalDiameter">The hydrant's nominal connection diameter in millimeters.</param>
    public record MapHydrantDto(int Id, double Longitude, double Latitude, HydrantStatus Status, int NominalDiameter);
}
