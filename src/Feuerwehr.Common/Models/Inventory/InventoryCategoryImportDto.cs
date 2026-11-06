using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Carries inventory category import data across the API boundary.
    /// </summary>
    public sealed class InventoryCategoryImportDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the inventory category import.
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the inventory category import.
        /// </summary>
        [Required(ErrorMessage = "Der Kategoriename ist erforderlich."), StringLength(100, ErrorMessage = "Der Kategoriename darf höchstens 100 Zeichen enthalten.")]
        public string Name { get; set; } = string.Empty;
    }
}
