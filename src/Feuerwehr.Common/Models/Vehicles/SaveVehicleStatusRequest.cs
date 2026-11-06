using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Specifies the input for save vehicle status operations.
    /// </summary>
    public sealed class SaveVehicleStatusRequest
    {
        /// <summary>
        /// Gets or sets the display name of the save vehicle status.
        /// </summary>
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether vehicles with this status can be reserved.
        /// </summary>
        public bool AllowsBooking { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the save vehicle status is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the version the caller last read; a mismatch rejects the update.
        /// </summary>
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
    }
}
