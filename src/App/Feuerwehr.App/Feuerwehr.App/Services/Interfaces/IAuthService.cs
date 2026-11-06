using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Defines the authentication operations required by application consumers.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates the supplied email and password and establishes a session subject to account activation and lockout checks.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="password">The password supplied for authentication.</param>
        /// <returns>A task whose result is true if authentication succeeds; otherwise, false.</returns>
        Task<bool> LoginAsync(string email, string password);

        /// <summary>
        /// Revokes the current session's refresh token and signs the user out.
        /// </summary>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task LogoutAsync();

        /// <summary>
        /// Renews the authenticated session after validating the refresh token and account state.
        /// </summary>
        /// <returns>A task whose result is true if the session is renewed; otherwise, false.</returns>
        Task<bool> RefreshTokenAsync();

        /// <summary>
        /// Retrieves the access token for authenticated API calls, renewing the session when needed.
        /// </summary>
        /// <returns>A task whose result is the current access token, or null if no valid authenticated session is available.</returns>
        Task<string?> GetAccessTokenAsync();

        /// <summary>
        /// Gets a value indicating whether a user is currently signed in.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Gets the signed-in user's email address.
        /// </summary>
        string? UserEmail { get; }

        /// <summary>
        /// Gets the signed-in user's full display name.
        /// </summary>
        string? UserFullName { get; }

        /// <summary>
        /// Gets the roles granted to the signed-in user.
        /// </summary>
        IReadOnlyList<string> UserRoles { get; }

        /// <summary>
        /// Occurs when the authentication state changes so subscribed views can refresh their permissions.
        /// </summary>
        event EventHandler? AuthStateChanged;
    }
}
