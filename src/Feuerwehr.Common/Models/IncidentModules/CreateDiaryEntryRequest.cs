using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Specifies the input for create diary entry operations.
    /// </summary>
    public class CreateDiaryEntryRequest
    {
        /// <summary>
        /// Gets or sets the client-generated command identifier used to detect retries.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC time of the event described by the diary entry.
        /// </summary>
        public DateTime EventTimestampUtc { get; set; }

        /// <summary>
        /// Gets or sets the category code used in equipment barcode allocation.
        /// </summary>
        [Required]
        [MaxLength(120)]
        public string CategoryCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the text content of the create diary entry.
        /// </summary>
        [Required]
        [MaxLength(4000)]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the configured sender email address.
        /// </summary>
        [MaxLength(200)]
        public string? Sender { get; set; }

        /// <summary>
        /// Gets or sets the destination email address.
        /// </summary>
        [MaxLength(200)]
        public string? Recipient { get; set; }

        /// <summary>
        /// Gets or sets the transmission channel recorded in the diary entry.
        /// </summary>
        [MaxLength(120)]
        public string? TransmissionType { get; set; }
    }
}
