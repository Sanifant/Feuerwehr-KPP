using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Specifies the input for create map symbol category operations.
    /// </summary>
    public sealed class CreateMapSymbolCategoryRequest
    {
        /// <summary>
        /// Gets or sets the display name of the create map symbol category.
        /// </summary>
        [Required, MaxLength(40)]
        public string Name { get; set; } = string.Empty;
    }
}
