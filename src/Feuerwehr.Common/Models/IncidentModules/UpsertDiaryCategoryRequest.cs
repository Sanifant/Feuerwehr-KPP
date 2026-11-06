using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Specifies the input for upsert diary category operations.
    /// </summary>
    public class UpsertDiaryCategoryRequest
    {
        /// <summary>
        /// Gets or sets the HTTP status code of the application outcome.
        /// </summary>
        [Required]
        [MaxLength(120)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the upsert diary category.
        /// </summary>
        [Required]
        [MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the upsert diary category is active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
