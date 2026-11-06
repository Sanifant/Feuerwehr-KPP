using de.openelp.feuerwehr.Common.Models.Auth;

namespace de.openelp.feuerwehr.Server.Services
{
    public interface IAuthService
    {
        Task<ServiceResult> Login(LoginRequest request);
        Task<ServiceResult> RefreshToken(RefreshTokenRequest request, string bearerToken);
        Task<ServiceResult> ResetPassword(ResetRequest request);
        Task<ServiceResult> ChangePassword(ChangePasswordRequest request, string? currentUserId);
        Task<ServiceResult> Logout(string? currentUserId);
        Task<ServiceResult> GetCurrentUser(string? currentUserId);
    }
}
