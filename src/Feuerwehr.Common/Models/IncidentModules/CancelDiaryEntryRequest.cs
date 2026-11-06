using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Specifies the input for cancel diary entry operations.
    /// </summary>
    public class CancelDiaryEntryRequest
    {
        /// <summary>
        /// Gets or sets the client-generated command identifier used to detect retries.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the diary revision the caller expects to update.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int ExpectedRevision { get; set; }

        /// <summary>
        /// Gets or sets the reason supplied when canceling the diary entry.
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
    }
}
