using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Specifies the input for save equipment operations.
    /// </summary>
    public sealed class SaveEquipmentRequest
    {
        /// <summary>
        /// Gets or sets the identifier of the equipment type defining its inspection plan.
        /// </summary>
        public Guid EquipmentTypeId { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer's serial number of the equipment.
        /// </summary>
        [Required, StringLength(150)] public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the equipment manufacturer.
        /// </summary>
        [Required, StringLength(150)] public string Manufacturer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the operational condition of the equipment.
        /// </summary>
        [EnumDataType(typeof(EquipmentStatus))] public EquipmentStatus Status { get; set; } = EquipmentStatus.Ready;

        /// <summary>
        /// Gets or sets the version the caller last read; a mismatch rejects the update.
        /// </summary>
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
    }
}
