namespace De.Openelp.Feuerwehr.Server.Models.Inventory
{
    /// <summary>
    /// Defines an equipment type and the inspection interval and instructions applied to its items.
    /// </summary>
    public sealed class EquipmentType
    {
        /// <summary>
        /// Gets or sets the unique identifier of the equipment type.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the category assigned to the equipment type.
        /// </summary>
        public Guid CategoryId { get; set; }

        /// <summary>
        /// Gets or sets the inventory category containing this equipment type.
        /// </summary>
        public InventoryCategory Category { get; set; } = null!;

        /// <summary>
        /// Gets or sets the display name of the equipment type.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the inspection interval in calendar months.
        /// </summary>
        public int InspectionIntervalMonths { get; set; }

        /// <summary>
        /// Gets or sets the instructions for performing the equipment inspection.
        /// </summary>
        public string InspectionInstructions { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the concurrency version used to detect conflicting updates.
        /// </summary>
        public int Version { get; set; } = 1;
    }
}
