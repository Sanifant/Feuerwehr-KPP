namespace De.Openelp.Feuerwehr.Common.Models.Auth
{
    /// <summary>
    /// Carries the result of login operations.
    /// </summary>
    public class LoginResponse
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
        /// Gets or sets the identifier of the user account.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's combined given and family names.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Identity roles assigned to the user.
        /// </summary>
        public List<string> Roles { get; set; } = new ();

        /// <summary>
        /// Gets or sets the access token expiry time.
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
}
