using De.Openelp.Feuerwehr.Common.Models.Auth;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the authentication operations required by application consumers.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates the supplied email and password and establishes a session subject to account activation and lockout checks.
        /// </summary>
        /// <param name="request">The login input to validate and apply.</param>
        /// <returns>A task whose result is an application result containing the access and refresh credentials and user profile, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Login(LoginRequest request);

        /// <summary>
        /// Renews the authenticated session after validating the refresh token and account state.
        /// </summary>
        /// <param name="request">The refresh token input to validate and apply.</param>
        /// <param name="bearerToken">The access token accompanying the refresh request.</param>
        /// <returns>A task whose result is an application result containing the renewed session credentials, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> RefreshToken(RefreshTokenRequest request, string bearerToken);

        /// <summary>
        /// Assigns a password after validating the invitation identifier and matching account email.
        /// </summary>
        /// <param name="request">The reset input to validate and apply.</param>
        /// <returns>A task whose result is an application result containing the password assignment or change outcome, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> ResetPassword(ResetRequest request);

        /// <summary>
        /// Verifies the current password with lockout tracking and changes the authenticated user's password.
        /// </summary>
        /// <param name="request">The change password input to validate and apply.</param>
        /// <param name="currentUserId">The authenticated caller's identifier, used for ownership and self-deletion checks.</param>
        /// <returns>A task whose result is an application result containing the password assignment or change outcome, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> ChangePassword(ChangePasswordRequest request, string? currentUserId);

        /// <summary>
        /// Revokes the current session's refresh token and signs the user out.
        /// </summary>
        /// <param name="currentUserId">The authenticated caller's identifier, used for ownership and self-deletion checks.</param>
        /// <returns>A task whose result is an application result containing the outcome of the authentication operation, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Logout(string? currentUserId);

        /// <summary>
        /// Retrieves the authenticated user's profile and assigned roles.
        /// </summary>
        /// <param name="currentUserId">The authenticated caller's identifier, used for ownership and self-deletion checks.</param>
        /// <returns>A task whose result is an application result containing the user profile and assigned roles, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetCurrentUser(string? currentUserId);
    }
}
