using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Specifies the input for create inventory category operations.
    /// </summary>
    public sealed class CreateInventoryCategoryRequest
    {
        /// <summary>
        /// Gets or sets the display name of the create inventory category.
        /// </summary>
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    }
}
