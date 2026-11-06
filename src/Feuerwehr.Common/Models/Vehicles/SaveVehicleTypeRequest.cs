using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Specifies the input for save vehicle type operations.
    /// </summary>
    public sealed class SaveVehicleTypeRequest
    {
        /// <summary>
        /// Gets or sets the display name of the save vehicle type.
        /// </summary>
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version the caller last read; a mismatch rejects the update.
        /// </summary>
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
    }
}
