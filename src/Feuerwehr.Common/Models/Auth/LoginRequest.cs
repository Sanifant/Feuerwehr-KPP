using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Auth
{
    /// <summary>
    /// Specifies the input for login operations.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password supplied for authentication.
        /// </summary>
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
