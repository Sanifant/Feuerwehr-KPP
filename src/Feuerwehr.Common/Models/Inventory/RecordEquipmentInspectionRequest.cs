using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Specifies the input for record equipment inspection operations.
    /// </summary>
    public sealed class RecordEquipmentInspectionRequest
    {
        /// <summary>
        /// Gets or sets the client-generated command identifier used to detect retries.
        /// </summary>
        public Guid CommandId { get; set; }

        /// <summary>
        /// Gets or sets the date on which the equipment inspection took place.
        /// </summary>
        public DateOnly InspectedOn { get; set; }

        /// <summary>
        /// Gets or sets the operational condition of the equipment.
        /// </summary>
        [EnumDataType(typeof(EquipmentStatus))] public EquipmentStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the comment recorded with the record equipment inspection.
        /// </summary>
        [StringLength(4000)] public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets the version the caller last read; a mismatch rejects the update.
        /// </summary>
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
    }
}
