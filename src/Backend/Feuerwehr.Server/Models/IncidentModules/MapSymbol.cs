using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    /// <summary>
    /// Defines a reusable situation-map symbol and its validated SVG markup.
    /// </summary>
    public sealed class MapSymbol
    {
        /// <summary>
        /// Gets or sets the unique identifier of the map symbol.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the map symbol.
        /// </summary>
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the category label assigned to the map symbol.
        /// </summary>
        [Required, MaxLength(40)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the validated SVG markup of the map symbol.
        /// </summary>
        [Required]
        public string SvgContent { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the map symbol is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the map symbol represents a vehicle.
        /// </summary>
        public bool IsVehicle { get; set; }

        /// <summary>
        /// Gets or sets the UTC time at which the record was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC time of the latest change.
        /// </summary>
        public DateTime UpdatedAtUtc { get; set; }
    }
}
