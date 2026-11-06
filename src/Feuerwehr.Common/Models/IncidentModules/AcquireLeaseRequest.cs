using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Specifies the input for acquire lease operations.
    /// </summary>
    public class AcquireLeaseRequest
    {
        /// <summary>
        /// Gets or sets the client session that owns or requests the editing lease.
        /// </summary>
        [Required]
        [MaxLength(120)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the client-generated command identifier used to detect retries.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string CommandId { get; set; } = string.Empty;
    }
}
