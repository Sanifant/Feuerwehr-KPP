using System.Security.Claims;
using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Validates authentication requests, account lockouts, token renewal, and password changes.
    /// </summary>
    public sealed class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserInvitationService _invitations;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthService" /> class with its required dependencies.
        /// </summary>
        /// <param name="userManager">The Identity user manager used to load and update accounts.</param>
        /// <param name="signInManager">The sign-in manager used to verify passwords and enforce account lockout.</param>
        /// <param name="invitations">The service that sends password invitations for newly created or re-invited users.</param>
        /// <param name="tokenService">The token service used to issue or revoke session credentials.</param>
        /// <param name="configuration">The application configuration containing connection, authentication, and frontend settings.</param>
        public AuthService(
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager,
                IUserInvitationService invitations,
                ITokenService tokenService,
                IConfiguration configuration)
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._invitations = invitations;
            this._tokenService = tokenService;
            this._configuration = configuration;
        }

        /// <summary>
        /// Authenticates the supplied email and password and establishes a session subject to account activation and lockout checks.
        /// </summary>
        /// <param name="request">The login input to validate and apply.</param>
        /// <returns>A task whose result is an application result containing the access and refresh credentials and user profile, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> Login(LoginRequest request)
        {

            ApplicationUser? user = await this._userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return ServiceResult.Unauthorized(new MessageResponse("Invalid credentials"));
            }

            if (user.PasswordResetRequired)
            {
                return ServiceResult.StatusCode(423, new MessageResponse("Das Konto ist bis zur Passwortvergabe über den Einladungslink gesperrt."));
            }

            SignInResult result = await this._signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return ServiceResult.StatusCode(423, new MessageResponse("Account locked due to multiple failed login attempts"));
            }

            if (!result.Succeeded)
            {
                return ServiceResult.Unauthorized(new MessageResponse("Invalid credentials"));
            }

            // Get user roles
            IList<string> roles = await this._userManager.GetRolesAsync(user);

            // Generate tokens
            string accessToken = await this._tokenService.GenerateAccessTokenAsync(user, roles);
            string refreshToken = this._tokenService.GenerateRefreshToken();

            // Store refresh token
            await this._tokenService.StoreRefreshTokenAsync(user.Id, refreshToken);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            _ = await this._userManager.UpdateAsync(user);

            IConfigurationSection jwtSettings = this._configuration.GetSection("JwtSettings");
            int expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15");

            return ServiceResult.Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Roles = roles.ToList(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
            });
        }

        /// <summary>
        /// Renews the authenticated session after validating the refresh token and account state.
        /// </summary>
        /// <param name="request">The refresh token input to validate and apply.</param>
        /// <param name="bearerToken">The access token accompanying the refresh request.</param>
        /// <returns>A task whose result is an application result containing the renewed session credentials, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> RefreshToken(RefreshTokenRequest request, string bearerToken)
        {

            // The access token might be expired but we need to extract the user ID
            string accessToken = bearerToken;

            if (string.IsNullOrEmpty(accessToken))
            {
                return ServiceResult.Unauthorized(new MessageResponse("Access token required"));
            }

            ClaimsPrincipal? principal = this._tokenService.GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
            {
                return ServiceResult.Unauthorized(new MessageResponse("Invalid access token"));
            }

            string? userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResult.Unauthorized(new MessageResponse("Invalid token claims"));
            }

            // Validate refresh token
            bool isValidRefreshToken = await this._tokenService.ValidateRefreshTokenAsync(userId, request.RefreshToken);
            if (!isValidRefreshToken)
            {
                return ServiceResult.Unauthorized(new MessageResponse("Invalid refresh token"));
            }

            ApplicationUser? user = await this._userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive || user.PasswordResetRequired)
            {
                return ServiceResult.Unauthorized(new MessageResponse("User not found or inactive"));
            }

            // Generate new tokens
            IList<string> roles = await this._userManager.GetRolesAsync(user);
            string newAccessToken = await this._tokenService.GenerateAccessTokenAsync(user, roles);
            string newRefreshToken = this._tokenService.GenerateRefreshToken();

            // Store new refresh token (implements rotation)
            await this._tokenService.StoreRefreshTokenAsync(userId, newRefreshToken);

            IConfigurationSection jwtSettings = this._configuration.GetSection("JwtSettings");
            int expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15");

            return ServiceResult.Ok(new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
            });
        }

        /// <summary>
        /// Assigns a password after validating the invitation identifier and matching account email.
        /// </summary>
        /// <param name="request">The reset input to validate and apply.</param>
        /// <returns>A task whose result is an application result containing the password assignment or change outcome, or validation, authorization, or conflict details on failure.</returns>
        public Task<ServiceResult> ResetPassword(ResetRequest request) => this._invitations.CompleteAsync(request);

        /// <summary>
        /// Verifies the current password with lockout tracking and changes the authenticated user's password.
        /// </summary>
        /// <param name="request">The change password input to validate and apply.</param>
        /// <param name="currentUserId">The authenticated caller's identifier, used for ownership and self-deletion checks.</param>
        /// <returns>A task whose result is an application result containing the password assignment or change outcome, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> ChangePassword(ChangePasswordRequest request, string? currentUserId)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return ServiceResult.Unauthorized();
            }

            ApplicationUser? user = await this._userManager.FindByIdAsync(currentUserId);
            if (user is null || !user.IsActive || user.PasswordResetRequired)
            {
                return ServiceResult.Unauthorized();
            }

            // Share the login attempt counter and honor existing account lockouts, including
            // requests with a still-valid bearer token. ChangePasswordAsync alone does neither.
            SignInResult passwordCheck = await this._signInManager.CheckPasswordSignInAsync(user, request.CurrentPassword, lockoutOnFailure: true);
            if (passwordCheck.IsLockedOut)
            {
                return ServiceResult.StatusCode(423, new MessageResponse("Das Konto ist nach mehreren Fehlversuchen vorübergehend gesperrt."));
            }

            if (!passwordCheck.Succeeded)
            {
                return ServiceResult.BadRequest(new MessageResponse("Das aktuelle Passwort konnte nicht bestätigt werden."));
            }

            IdentityResult result = await this._userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(error => error.Code == "PasswordMismatch"))
                {
                    return ServiceResult.BadRequest(new MessageResponse("Das aktuelle Passwort ist falsch."));
                }

                return ServiceResult.BadRequest(new IdentityErrorResponse(
                    "Das Passwort konnte nicht geändert werden.",
                    result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
            }

            await this._tokenService.RevokeRefreshTokenAsync(user.Id);
            return ServiceResult.Ok(new MessageResponse("Ihr Passwort wurde geändert."));
        }

        /// <summary>
        /// Revokes the current session's refresh token and signs the user out.
        /// </summary>
        /// <param name="currentUserId">The authenticated caller's identifier, used for ownership and self-deletion checks.</param>
        /// <returns>A task whose result is an application result containing the outcome of the authentication operation, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> Logout(string? currentUserId)
        {
            string? userId = currentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResult.BadRequest(new MessageResponse("Invalid user"));
            }

            await this._tokenService.RevokeRefreshTokenAsync(userId);
            await this._signInManager.SignOutAsync();

            return ServiceResult.Ok(new MessageResponse("Logged out successfully"));
        }

        /// <summary>
        /// Retrieves the authenticated user's profile and assigned roles.
        /// </summary>
        /// <param name="currentUserId">The authenticated caller's identifier, used for ownership and self-deletion checks.</param>
        /// <returns>A task whose result is an application result containing the user profile and assigned roles, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> GetCurrentUser(string? currentUserId)
        {
            string? userId = currentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResult.Unauthorized();
            }

            ApplicationUser? user = await this._userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.NotFound();
            }

            IList<string> roles = await this._userManager.GetRolesAsync(user);

            return ServiceResult.Ok(new UserInfo { Id = user.Id, Email = user.Email ?? string.Empty, FirstName = user.FirstName, LastName = user.LastName, FullName = user.FullName, FireDepartmentId = user.FireDepartmentId, Roles = roles.ToList() });
        }
    }
}
