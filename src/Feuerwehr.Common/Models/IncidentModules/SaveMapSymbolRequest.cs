using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Specifies the input for save map symbol operations.
    /// </summary>
    public sealed class SaveMapSymbolRequest
    {
        /// <summary>
        /// Gets or sets the display name of the save map symbol.
        /// </summary>
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the category label assigned to the save map symbol.
        /// </summary>
        [Required, MaxLength(40)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the validated SVG markup of the map symbol.
        /// </summary>
        [Required, MaxLength(262144)]
        public string SvgContent { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the save map symbol is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the map symbol represents a vehicle.
        /// </summary>
        public bool IsVehicle { get; set; }
    }
}
