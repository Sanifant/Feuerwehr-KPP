using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.Server.Models.Inventory
{
    /// <summary>
    /// Stores an equipment inspection and the inspection-plan snapshot effective when it was recorded.
    /// </summary>
    public sealed class EquipmentInspection
    {
        /// <summary>
        /// Gets or sets the unique identifier of the equipment inspection.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the equipment item.
        /// </summary>
        public Guid EquipmentId { get; set; }

        /// <summary>
        /// Gets or sets the equipment record associated with this equipment inspection.
        /// </summary>
        public Equipment Equipment { get; set; } = null!;

        /// <summary>
        /// Gets or sets the client-generated command identifier used to detect retries.
        /// </summary>
        public Guid CommandId { get; set; }

        /// <summary>
        /// Gets or sets the date on which the equipment inspection took place.
        /// </summary>
        public DateOnly InspectedOn { get; set; }

        /// <summary>
        /// Gets or sets the UTC time at which this record was saved.
        /// </summary>
        public DateTime RecordedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who recorded the inspection.
        /// </summary>
        public string InspectorId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the inspector's display name captured with the inspection.
        /// </summary>
        public string InspectorName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the comment recorded with the equipment inspection.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets the operational condition of the equipment.
        /// </summary>
        public EquipmentStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the display name of the equipment type.
        /// </summary>
        public string EquipmentTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the inspection interval in calendar months.
        /// </summary>
        public int InspectionIntervalMonths { get; set; }

        /// <summary>
        /// Gets or sets the instructions for performing the equipment inspection.
        /// </summary>
        public string InspectionInstructions { get; set; } = string.Empty;
    }
}
