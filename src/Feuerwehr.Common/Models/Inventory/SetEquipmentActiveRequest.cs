using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Specifies the input for set equipment active operations.
    /// </summary>
    public sealed class SetEquipmentActiveRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether the set equipment active is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the version the caller last read; a mismatch rejects the update.
        /// </summary>
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
    }
}
