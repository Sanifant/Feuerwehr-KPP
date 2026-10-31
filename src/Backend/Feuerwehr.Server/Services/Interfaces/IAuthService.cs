using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Identity;

namespace De.Openelp.Feuerwehr.Server.Services
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
