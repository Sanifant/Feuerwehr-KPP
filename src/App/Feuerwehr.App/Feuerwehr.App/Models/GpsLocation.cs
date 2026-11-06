namespace De.Openelp.Feuerwehr.App.Models
{
    /// <summary>
    /// A device location in WGS84 coordinates (degrees).
    /// </summary>
    public sealed class GpsLocation
    {
        /// <summary>
        /// Gets the latitude in decimal degrees.
        /// </summary>
        public double Latitude { get; init; }

        /// <summary>
        /// Gets the longitude in decimal degrees.
        /// </summary>
        public double Longitude { get; init; }
    }
}
