using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.Server.Models.Inventory
{
    /// <summary>
    /// Stores an equipment item, its unique barcode, activation state, and inspection schedule.
    /// </summary>
    public sealed class Equipment
    {
        /// <summary>
        /// Gets or sets the unique identifier of the equipment.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the numeric equipment barcode sequence value.
        /// </summary>
        public int BarcodeNumber { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the equipment type defining its inspection plan.
        /// </summary>
        public Guid EquipmentTypeId { get; set; }

        /// <summary>
        /// Gets or sets the equipment type providing the inspection plan.
        /// </summary>
        public EquipmentType EquipmentType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the manufacturer's serial number of the equipment.
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the equipment manufacturer.
        /// </summary>
        public string Manufacturer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the operational condition of the equipment.
        /// </summary>
        public EquipmentStatus Status { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the equipment is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the date the equipment was registered.
        /// </summary>
        public DateOnly CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the most recent inspection date, or null when no inspection has been recorded.
        /// </summary>
        public DateOnly? LastInspectedOn { get; set; }

        /// <summary>
        /// Gets or sets the concurrency version used to detect conflicting updates.
        /// </summary>
        public int Version { get; set; } = 1;
    }
}
