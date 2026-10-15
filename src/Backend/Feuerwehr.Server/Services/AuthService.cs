using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Common.Models.IncidentModules;
using de.openelp.feuerwehr.Server.Models;
using de.openelp.feuerwehr.Common.Models.Auth;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.AspNetCore.Identity;

namespace de.openelp.feuerwehr.Server.Services;

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

    public async Task<ServiceResult> Login(LoginRequest request)
    {

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            return ServiceResult.Unauthorized(new MessageResponse("Invalid credentials"));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return ServiceResult.StatusCode(423, new MessageResponse("Account locked due to multiple failed login attempts"));
        }

        if (!result.Succeeded)
        {
            return ServiceResult.Unauthorized(new MessageResponse("Invalid credentials"));
        }

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        // Generate tokens
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token
        await _tokenService.StoreRefreshTokenAsync(user.Id, refreshToken);

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15");

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

    public async Task<ServiceResult> RefreshToken(RefreshTokenRequest request, string bearerToken)
    {

        // The access token might be expired but we need to extract the user ID
        var accessToken = bearerToken;

        if (string.IsNullOrEmpty(accessToken))
        {
            return ServiceResult.Unauthorized(new MessageResponse("Access token required"));
        }

        var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
        if (principal == null)
        {
            return ServiceResult.Unauthorized(new MessageResponse("Invalid access token"));
        }

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return ServiceResult.Unauthorized(new MessageResponse("Invalid token claims"));
        }

        // Validate refresh token
        var isValidRefreshToken = await _tokenService.ValidateRefreshTokenAsync(userId, request.RefreshToken);
        if (!isValidRefreshToken)
        {
            return ServiceResult.Unauthorized(new MessageResponse("Invalid refresh token"));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return ServiceResult.Unauthorized(new MessageResponse("User not found or inactive"));
        }

        // Generate new tokens
        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = await _tokenService.GenerateAccessTokenAsync(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        // Store new refresh token (implements rotation)
        await _tokenService.StoreRefreshTokenAsync(userId, newRefreshToken);

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15");

        return ServiceResult.Ok(new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        });

    }

    public Task<ServiceResult> ResetPassword(ResetRequest request) => _invitations.CompleteAsync(request);

    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>

    public async Task<ServiceResult> Logout(string? currentUserId)
    {
        var userId = currentUserId;
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

    public async Task<ServiceResult> GetCurrentUser(string? currentUserId)
    {
        var userId = currentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return ServiceResult.Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult.NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return ServiceResult.Ok(new UserInfo { Id = user.Id, Email = user.Email ?? string.Empty, FirstName = user.FirstName, LastName = user.LastName, FullName = user.FullName, FireDepartmentId = user.FireDepartmentId, Roles = roles.ToList() });

    }

}

public interface IAuthService
{
    Task<ServiceResult> Login(LoginRequest request);
    Task<ServiceResult> RefreshToken(RefreshTokenRequest request, string bearerToken);
    Task<ServiceResult> ResetPassword(ResetRequest request);
    Task<ServiceResult> Logout(string? currentUserId);
    Task<ServiceResult> GetCurrentUser(string? currentUserId);
}
