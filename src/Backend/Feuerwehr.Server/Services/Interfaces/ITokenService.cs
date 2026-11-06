using System.Security.Claims;
using De.Openelp.Feuerwehr.Server.Models;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the token operations required by application consumers.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Creates a signed JWT containing user details, roles, department, and the current security stamp.
        /// </summary>
        /// <param name="user">The user account involved in the operation.</param>
        /// <param name="roles">The Identity roles assigned to the user.</param>
        /// <returns>A task whose result is the signed JWT access token.</returns>
        Task<string> GenerateAccessTokenAsync(ApplicationUser user, IList<string> roles);

        /// <summary>
        /// Creates a cryptographically random token for renewing the authenticated session.
        /// </summary>
        /// <returns>The random refresh token encoded as Base64.</returns>
        string GenerateRefreshToken();

        /// <summary>
        /// Compares the supplied refresh token with the hash stored for the user account.
        /// </summary>
        /// <param name="userId">The identifier of the user account.</param>
        /// <param name="refreshToken">The token used to renew the authenticated session.</param>
        /// <returns>A task whose result is true if the supplied refresh token matches the user's stored token hash; otherwise, false.</returns>
        Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);

        /// <summary>
        /// Replaces the user's stored refresh token with the hash of the supplied token.
        /// </summary>
        /// <param name="userId">The identifier of the user account.</param>
        /// <param name="refreshToken">The token used to renew the authenticated session.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task StoreRefreshTokenAsync(string userId, string refreshToken);

        /// <summary>
        /// Removes the user's stored refresh token to prevent further session renewal.
        /// </summary>
        /// <param name="userId">The identifier of the user account.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task RevokeRefreshTokenAsync(string userId);

        /// <summary>
        /// Checks the token's user account, activation state, and security stamp against current Identity data.
        /// </summary>
        /// <param name="principal">The authenticated token principal whose account and security stamp are checked.</param>
        /// <returns>A task whose result is true if the account remains active and its security stamp matches the token; otherwise, false.</returns>
        Task<bool> IsAccessTokenValidAsync(ClaimsPrincipal principal);

        /// <summary>
        /// Validates the JWT signature, issuer, and audience while allowing an expired lifetime for session renewal.
        /// </summary>
        /// <param name="token">The opaque invitation identifier used for password assignment.</param>
        /// <returns>The validated token principal, or null if signature, issuer, audience, or algorithm validation fails.</returns>
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
