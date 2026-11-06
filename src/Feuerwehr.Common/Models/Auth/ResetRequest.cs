using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Auth
{
    /// <summary>
    /// Specifies the input for reset operations.
    /// </summary>
    public class ResetRequest
    {
        /// <summary>
        /// Gets or sets the opaque invitation identifier used for password assignment.
        /// </summary>
        [Required]
        [RegularExpression(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new password to validate and assign to the account.
        /// </summary>
        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
