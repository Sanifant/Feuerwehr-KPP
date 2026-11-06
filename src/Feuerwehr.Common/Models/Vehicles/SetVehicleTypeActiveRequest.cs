using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Specifies the input for set vehicle type active operations.
    /// </summary>
    public sealed class SetVehicleTypeActiveRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether the set vehicle type active is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the version the caller last read; a mismatch rejects the update.
        /// </summary>
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
    }
}
