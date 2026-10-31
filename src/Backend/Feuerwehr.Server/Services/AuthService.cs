using System.Security.Claims;
using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Identity;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserInvitationService _invitations;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;

        public AuthService(
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager,
                IUserInvitationService invitations,
                ITokenService tokenService,
                IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _invitations = invitations;
            _tokenService = tokenService;
            _configuration = configuration;
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        /// <param name="request">The request.</param>

        public async Task<ServiceResult> Login(LoginRequest request)
        {

            ApplicationUser? user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return ServiceResult.Unauthorized(new MessageResponse("Invalid credentials"));
            }

            if (user.PasswordResetRequired)
                return ServiceResult.StatusCode(423, new MessageResponse("Das Konto ist bis zur Passwortvergabe über den Einladungslink gesperrt."));

            SignInResult result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return ServiceResult.StatusCode(423, new MessageResponse("Account locked due to multiple failed login attempts"));
            }

            if (!result.Succeeded)
            {
                return ServiceResult.Unauthorized(new MessageResponse("Invalid credentials"));
            }

            // Get user roles
            IList<string> roles = await _userManager.GetRolesAsync(user);

            // Generate tokens
            string accessToken = await _tokenService.GenerateAccessTokenAsync(user, roles);
            string refreshToken = _tokenService.GenerateRefreshToken();

            // Store refresh token
            await _tokenService.StoreRefreshTokenAsync(user.Id, refreshToken);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            IConfigurationSection jwtSettings = _configuration.GetSection("JwtSettings");
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
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="bearerToken">The bearer Token.</param>

        public async Task<ServiceResult> RefreshToken(RefreshTokenRequest request, string bearerToken)
        {

            // The access token might be expired but we need to extract the user ID
            string accessToken = bearerToken;

            if (string.IsNullOrEmpty(accessToken))
            {
                return ServiceResult.Unauthorized(new MessageResponse("Access token required"));
            }

            ClaimsPrincipal? principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
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
            bool isValidRefreshToken = await _tokenService.ValidateRefreshTokenAsync(userId, request.RefreshToken);
            if (!isValidRefreshToken)
            {
                return ServiceResult.Unauthorized(new MessageResponse("Invalid refresh token"));
            }

            ApplicationUser? user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive || user.PasswordResetRequired)
            {
                return ServiceResult.Unauthorized(new MessageResponse("User not found or inactive"));
            }

            // Generate new tokens
            IList<string> roles = await _userManager.GetRolesAsync(user);
            string newAccessToken = await _tokenService.GenerateAccessTokenAsync(user, roles);
            string newRefreshToken = _tokenService.GenerateRefreshToken();

            // Store new refresh token (implements rotation)
            await _tokenService.StoreRefreshTokenAsync(userId, newRefreshToken);

            IConfigurationSection jwtSettings = _configuration.GetSection("JwtSettings");
            int expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15");

            return ServiceResult.Ok(new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
            });

        }

        /// <summary>
        /// Executes reset Password.
        /// </summary>
        public Task<ServiceResult> ResetPassword(ResetRequest request) => _invitations.CompleteAsync(request);

        /// <summary>
        /// Executes change Password.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="currentUserId">The current User Id.</param>
        public async Task<ServiceResult> ChangePassword(ChangePasswordRequest request, string? currentUserId)
        {
            if (string.IsNullOrWhiteSpace(currentUserId)) return ServiceResult.Unauthorized();
            ApplicationUser? user = await _userManager.FindByIdAsync(currentUserId);
            if (user is null || !user.IsActive || user.PasswordResetRequired) return ServiceResult.Unauthorized();

            // Share the login attempt counter and honor existing account lockouts, including
            // requests with a still-valid bearer token. ChangePasswordAsync alone does neither.
            SignInResult passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, request.CurrentPassword, lockoutOnFailure: true);
            if (passwordCheck.IsLockedOut)
                return ServiceResult.StatusCode(423, new MessageResponse("Das Konto ist nach mehreren Fehlversuchen vorübergehend gesperrt."));
            if (!passwordCheck.Succeeded)
                return ServiceResult.BadRequest(new MessageResponse("Das aktuelle Passwort konnte nicht bestätigt werden."));

            IdentityResult result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(error => error.Code == "PasswordMismatch"))
                    return ServiceResult.BadRequest(new MessageResponse("Das aktuelle Passwort ist falsch."));
                return ServiceResult.BadRequest(new IdentityErrorResponse("Das Passwort konnte nicht geändert werden.",
                    result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
            }

            await _tokenService.RevokeRefreshTokenAsync(user.Id);
            return ServiceResult.Ok(new MessageResponse("Ihr Passwort wurde geändert."));
        }

        /// <summary>
        /// Logout and revoke refresh token
        /// </summary>
        /// <param name="currentUserId">The current User Id.</param>

        public async Task<ServiceResult> Logout(string? currentUserId)
        {
            string? userId = currentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResult.BadRequest(new MessageResponse("Invalid user"));
            }

            await _tokenService.RevokeRefreshTokenAsync(userId);
            await _signInManager.SignOutAsync();

            return ServiceResult.Ok(new MessageResponse("Logged out successfully"));

        }

        /// <summary>
        /// Get current user info
        /// </summary>
        /// <param name="currentUserId">The current User Id.</param>

        public async Task<ServiceResult> GetCurrentUser(string? currentUserId)
        {
            string? userId = currentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResult.Unauthorized();
            }

            ApplicationUser? user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.NotFound();
            }

            IList<string> roles = await _userManager.GetRolesAsync(user);

            return ServiceResult.Ok(new UserInfo { Id = user.Id, Email = user.Email ?? string.Empty, FirstName = user.FirstName, LastName = user.LastName, FullName = user.FullName, FireDepartmentId = user.FireDepartmentId, Roles = roles.ToList() });

        }

    }
}
