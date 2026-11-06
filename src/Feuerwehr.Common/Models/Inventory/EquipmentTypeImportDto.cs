using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Carries equipment type import data across the API boundary.
    /// </summary>
    public sealed class EquipmentTypeImportDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the equipment type import.
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the category assigned to the equipment type import.
        /// </summary>
        public Guid? CategoryId { get; set; }

        /// <summary>
        /// Gets or sets the category's display name.
        /// </summary>
        [StringLength(100, ErrorMessage = "Der Kategoriename darf höchstens 100 Zeichen enthalten.")] public string? CategoryName { get; set; }

        /// <summary>
        /// Gets or sets the display name of the equipment type import.
        /// </summary>
        [Required(ErrorMessage = "Die Typbezeichnung ist erforderlich."), StringLength(150, ErrorMessage = "Die Typbezeichnung darf höchstens 150 Zeichen enthalten.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the inspection interval in calendar months.
        /// </summary>
        [Range(1, 120, ErrorMessage = "Das Prüfintervall muss zwischen 1 und 120 Monaten liegen.")] public int InspectionIntervalMonths { get; set; }

        /// <summary>
        /// Gets or sets the instructions for performing the equipment inspection.
        /// </summary>
        [Required(ErrorMessage = "Eine Prüfanweisung ist erforderlich."), StringLength(8000, ErrorMessage = "Die Prüfanweisung darf höchstens 8000 Zeichen enthalten.")]
        public string InspectionInstructions { get; set; } = string.Empty;
    }
}
