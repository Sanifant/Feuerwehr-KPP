using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    /// <summary>
    /// Defines a category available when classifying incident diary entries.
    /// </summary>
    public class DiaryCategory
    {
        /// <summary>
        /// Gets or sets the unique identifier of the diary category.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code of the application outcome.
        /// </summary>
        [MaxLength(120)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the diary category.
        /// </summary>
        [MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the diary category is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the UTC time at which the record was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC time of the latest change.
        /// </summary>
        public DateTime UpdatedAtUtc { get; set; }
    }
}
