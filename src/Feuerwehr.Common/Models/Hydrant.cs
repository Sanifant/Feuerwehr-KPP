using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models
{

    /// <summary>
    /// Describes a hydrant's location, water supply, nominal diameter, and operational condition in the API.
    /// </summary>
    public class Hydrant
    {
        /// <summary>
        /// Gets or sets the unique identifier of the hydrant.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the street address of the hydrant.
        /// </summary>
        public Address Address { get; set; } = new ();

        /// <summary>
        /// Gets or sets the latitude in decimal degrees.
        /// </summary>
        [Required]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude in decimal degrees.
        /// </summary>
        [Required]
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the hydrant's nominal connection diameter in millimeters.
        /// </summary>
        [Required]
        public int NominalDiameter { get; set; }

        /// <summary>
        /// Gets or sets the kind of hydrant.
        /// </summary>
        public HydrantType Type { get; set; }

        /// <summary>
        /// Gets or sets the hydrant's water supply source.
        /// </summary>
        public WaterSource WaterSource { get; set; }

        /// <summary>
        /// Gets or sets the current state of the hydrant.
        /// </summary>
        public HydrantStatus Status { get; set; }

        /// <summary>
        /// Gets or sets optional notes about the hydrant.
        /// </summary>
        public string? Notes { get; set; }

        // Computed Read-Only Properties (Excluded from Database Mapping)
        /// <summary>
        /// Gets a value indicating whether the hydrant is an underground hydrant.
        /// </summary>
        public bool IsUnderground => this.Type == HydrantType.Underground;

        /// <summary>
        /// Gets a value indicating whether the hydrant is available for operational use.
        /// </summary>
        public bool IsOperational => this.Status == HydrantStatus.Operational;

        /// <summary>
        /// Gets the formatted DIN nominal-diameter label.
        /// </summary>
        public string DiameterDescription => $"DIN {this.NominalDiameter}";
    }
}
