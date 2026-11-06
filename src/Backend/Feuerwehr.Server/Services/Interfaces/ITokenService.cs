using System.Security.Claims;
using de.openelp.feuerwehr.Server.Models;

namespace de.openelp.feuerwehr.Server.Services
{
    public interface ITokenService
    {
        Task<string> GenerateAccessTokenAsync(ApplicationUser user, IList<string> roles);
        string GenerateRefreshToken();
        Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
        Task StoreRefreshTokenAsync(string userId, string refreshToken);
        Task RevokeRefreshTokenAsync(string userId);
        Task<bool> IsAccessTokenValidAsync(ClaimsPrincipal principal);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
