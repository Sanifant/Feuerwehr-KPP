using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Specifies the input for save equipment type operations.
    /// </summary>
    public sealed class SaveEquipmentTypeRequest
    {
        /// <summary>
        /// Gets or sets the identifier of the category assigned to the save equipment type.
        /// </summary>
        public Guid CategoryId { get; set; }

        /// <summary>
        /// Gets or sets the display name of the save equipment type.
        /// </summary>
        [Required, StringLength(150)] public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the inspection interval in calendar months.
        /// </summary>
        [Range(1, 120)] public int InspectionIntervalMonths { get; set; } = 12;

        /// <summary>
        /// Gets or sets the instructions for performing the equipment inspection.
        /// </summary>
        [Required, StringLength(8000)] public string InspectionInstructions { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version the caller last read; a mismatch rejects the update.
        /// </summary>
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
    }
}
