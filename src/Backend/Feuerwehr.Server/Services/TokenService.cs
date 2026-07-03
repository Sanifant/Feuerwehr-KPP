using Feuerwehr.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Feuerwehr.Server.Services
{
    public interface ITokenService
    {
        Task<string> GenerateAccessTokenAsync(ApplicationUser user, IList<string> roles);
        string GenerateRefreshToken();
        Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
        Task StoreRefreshTokenAsync(string userId, string refreshToken);
        Task RevokeRefreshTokenAsync(string userId);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;

        public TokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<string> GenerateAccessTokenAsync(ApplicationUser user, IList<string> roles)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
            var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
            var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add roles as claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add FireDepartmentId if exists
            if (user.FireDepartmentId.HasValue)
            {
                claims.Add(new Claim("departmentId", user.FireDepartmentId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Get stored refresh token from Identity's token store
            var storedToken = await _userManager.GetAuthenticationTokenAsync(user, "FeuerwehrApp", "RefreshToken");

            if (string.IsNullOrEmpty(storedToken)) return false;

            // Hash the provided token and compare with stored hash
            var hashedProvidedToken = HashToken(refreshToken);
            return storedToken == hashedProvidedToken;
        }

        public async Task StoreRefreshTokenAsync(string userId, string refreshToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new InvalidOperationException("User not found");

            // Hash the refresh token before storing
            var hashedToken = HashToken(refreshToken);

            // Remove old token
            await _userManager.RemoveAuthenticationTokenAsync(user, "FeuerwehrApp", "RefreshToken");

            // Store new hashed token
            await _userManager.SetAuthenticationTokenAsync(user, "FeuerwehrApp", "RefreshToken", hashedToken);
        }

        public async Task RevokeRefreshTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            await _userManager.RemoveAuthenticationTokenAsync(user, "FeuerwehrApp", "RefreshToken");
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // Don't validate expiration for refresh
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
