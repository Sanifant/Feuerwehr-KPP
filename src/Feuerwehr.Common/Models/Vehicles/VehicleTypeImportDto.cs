using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Carries vehicle type import data across the API boundary.
    /// </summary>
    public sealed class VehicleTypeImportDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the vehicle type import.
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the vehicle type import.
        /// </summary>
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the vehicle type import is active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
