using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Specifies the input for delete map element operations.
    /// </summary>
    public class DeleteMapElementRequest
    {
        /// <summary>
        /// Gets or sets the client-generated command identifier used to detect retries.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the client session that owns or requests the editing lease.
        /// </summary>
        [Required]
        [MaxLength(120)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the opaque token proving ownership of the editing lease.
        /// </summary>
        [Required]
        [MaxLength(120)]
        public string LeaseToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version the caller last read; a mismatch rejects the update.
        /// </summary>
        public int ExpectedVersion { get; set; }
    }
}
