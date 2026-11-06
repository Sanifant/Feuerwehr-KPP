namespace De.Openelp.Feuerwehr.Common.Models.Auth
{
    /// <summary>
    /// Carries the result of token operations.
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// Gets or sets the JWT used to authorize API requests.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token used to renew the authenticated session.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the access token expiry time.
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
}
