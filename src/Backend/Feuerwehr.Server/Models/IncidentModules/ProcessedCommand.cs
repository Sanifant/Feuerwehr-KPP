using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    /// <summary>
    /// Stores a command's payload hash and serialized response to support consistent idempotent retries.
    /// </summary>
    public class ProcessedCommand
    {
        /// <summary>
        /// Gets or sets the unique identifier of the processed command.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the client-generated command identifier used to detect retries.
        /// </summary>
        [MaxLength(200)]
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the user account.
        /// </summary>
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the operation scope used to distinguish idempotent commands.
        /// </summary>
        [MaxLength(100)]
        public string Scope { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the associated target.
        /// </summary>
        [MaxLength(200)]
        public string? TargetId { get; set; }

        /// <summary>
        /// Gets or sets the hash of the command payload used to detect inconsistent retries.
        /// </summary>
        [MaxLength(200)]
        public string PayloadHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTTP status code retained for command replay.
        /// </summary>
        public int ResponseStatusCode { get; set; }

        /// <summary>
        /// Gets or sets the serialized response retained for command replay.
        /// </summary>
        public string ResponseJson { get; set; } = "{}";

        /// <summary>
        /// Gets or sets the UTC time at which the record was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }
    }
}
