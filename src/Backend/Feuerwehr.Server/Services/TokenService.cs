using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using De.Openelp.Feuerwehr.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace De.Openelp.Feuerwehr.Server.Services
{

    /// <summary>
    /// Issues JWT access tokens and manages hashed refresh tokens and security-stamp validation.
    /// </summary>
    public class TokenService : ITokenService
    {
        /// <summary>
        /// The JWT claim name containing the account security stamp.
        /// </summary>
        public const string SecurityStampClaim = "security_stamp";
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenService" /> class with its required dependencies.
        /// </summary>
        /// <param name="configuration">The application configuration containing connection, authentication, and frontend settings.</param>
        /// <param name="userManager">The Identity user manager used to load and update accounts.</param>
        public TokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            this._configuration = configuration;
            this._userManager = userManager;
        }

        /// <summary>
        /// Creates a signed JWT containing user details, roles, department, and the current security stamp.
        /// </summary>
        /// <param name="user">The user account involved in the operation.</param>
        /// <param name="roles">The Identity roles assigned to the user.</param>
        /// <returns>A task whose result is the signed JWT access token.</returns>
        public async Task<string> GenerateAccessTokenAsync(ApplicationUser user, IList<string> roles)
        {
            IConfigurationSection jwtSettings = this._configuration.GetSection("JwtSettings");
            string secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            string issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
            string audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
            int expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15");

            List<Claim> claims = new ()
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName),
                new Claim(SecurityStampClaim, await this._userManager.GetSecurityStampAsync(user)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add roles as claims
            foreach (string role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add FireDepartmentId if exists
            if (user.FireDepartmentId.HasValue)
            {
                claims.Add(new Claim("departmentId", user.FireDepartmentId.Value.ToString()));
            }

            SymmetricSecurityKey key = new (Encoding.UTF8.GetBytes(secretKey));
            SigningCredentials credentials = new (key, SecurityAlgorithms.HmacSha256);
            DateTime expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

            JwtSecurityToken token = new (
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Creates a cryptographically random token for renewing the authenticated session.
        /// </summary>
        /// <returns>The random refresh token encoded as Base64.</returns>
        public string GenerateRefreshToken()
        {
            byte[] randomNumber = new byte[64];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// Checks the token's user account, activation state, and security stamp against current Identity data.
        /// </summary>
        /// <param name="principal">The authenticated token principal whose account and security stamp are checked.</param>
        /// <returns>A task whose result is true if the account remains active and its security stamp matches the token; otherwise, false.</returns>
        public async Task<bool> IsAccessTokenValidAsync(ClaimsPrincipal principal)
        {
            string? userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            string? stamp = principal.FindFirstValue(SecurityStampClaim);
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(stamp))
            {
                return false;
            }

            ApplicationUser? user = await this._userManager.FindByIdAsync(userId);
            return user is { IsActive: true, PasswordResetRequired: false } &&
                string.Equals(stamp, await this._userManager.GetSecurityStampAsync(user), StringComparison.Ordinal);
        }

        /// <summary>
        /// Compares the supplied refresh token with the hash stored for the user account.
        /// </summary>
        /// <param name="userId">The identifier of the user account.</param>
        /// <param name="refreshToken">The token used to renew the authenticated session.</param>
        /// <returns>A task whose result is true if the supplied refresh token matches the user's stored token hash; otherwise, false.</returns>
        public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
        {
            ApplicationUser? user = await this._userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Get stored refresh token from Identity's token store
            string? storedToken = await this._userManager.GetAuthenticationTokenAsync(user, "FeuerwehrApp", "RefreshToken");

            if (string.IsNullOrEmpty(storedToken))
            {
                return false;
            }

            // Hash the provided token and compare with stored hash
            string hashedProvidedToken = this.HashToken(refreshToken);
            return storedToken == hashedProvidedToken;
        }

        /// <summary>
        /// Replaces the user's stored refresh token with the hash of the supplied token.
        /// </summary>
        /// <param name="userId">The identifier of the user account.</param>
        /// <param name="refreshToken">The token used to renew the authenticated session.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task StoreRefreshTokenAsync(string userId, string refreshToken)
        {
            ApplicationUser? user = await this._userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Hash the refresh token before storing
            string hashedToken = this.HashToken(refreshToken);

            // Remove old token
            _ = await this._userManager.RemoveAuthenticationTokenAsync(user, "FeuerwehrApp", "RefreshToken");

            // Store new hashed token
            _ = await this._userManager.SetAuthenticationTokenAsync(user, "FeuerwehrApp", "RefreshToken", hashedToken);
        }

        /// <summary>
        /// Removes the user's stored refresh token to prevent further session renewal.
        /// </summary>
        /// <param name="userId">The identifier of the user account.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task RevokeRefreshTokenAsync(string userId)
        {
            ApplicationUser? user = await this._userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return;
            }

            _ = await this._userManager.RemoveAuthenticationTokenAsync(user, "FeuerwehrApp", "RefreshToken");
        }

        /// <summary>
        /// Validates the JWT signature, issuer, and audience while allowing an expired lifetime for session renewal.
        /// </summary>
        /// <param name="token">The opaque invitation identifier used for password assignment.</param>
        /// <returns>The validated token principal, or null if signature, issuer, audience, or algorithm validation fails.</returns>
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            IConfigurationSection jwtSettings = this._configuration.GetSection("JwtSettings");
            string secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

            TokenValidationParameters tokenValidationParameters = new ()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // Don't validate expiration for refresh
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };

            JwtSecurityTokenHandler tokenHandler = new ();
            try
            {
                ClaimsPrincipal principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken? securityToken);

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
            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
