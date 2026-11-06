namespace De.Openelp.Feuerwehr.App.Models
{
    /// <summary>
    /// Represents a selectable hydrant marker on the client map.
    /// </summary>
    public class HydrantPin
    {
        /// <summary>
        /// Gets or sets the title displayed for the hydrant pin.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the hydrant connection diameter.
        /// </summary>
        public string Diameter { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the latitude in decimal degrees.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude in decimal degrees.
        /// </summary>
        public double Longitude { get; set; }
    }
}
