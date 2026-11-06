using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Auth
{
    /// <summary>
    /// Specifies the input for refresh token operations.
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// Gets or sets the token used to renew the authenticated session.
        /// </summary>
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
