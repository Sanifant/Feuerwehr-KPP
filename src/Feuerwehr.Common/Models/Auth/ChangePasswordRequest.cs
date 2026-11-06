using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Auth
{
    /// <summary>
    /// Specifies the input for change password operations.
    /// </summary>
    public class ChangePasswordRequest
    {
        /// <summary>
        /// Gets or sets the current password to verify before changing it.
        /// </summary>
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new password to validate and assign to the account.
        /// </summary>
        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
