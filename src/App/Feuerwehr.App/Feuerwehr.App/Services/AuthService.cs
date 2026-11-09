using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using de.openelp.feuerwehr.Common.Models.Auth;

namespace de.openelp.feuerwehr.App.Services
{
    public class AuthService : IAuthService
    {
        private const string AccessTokenKey = "access_token";
        private const string RefreshTokenKey = "refresh_token";

        private const string emailClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

        private const string nameClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

        private const string roleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

        private readonly HttpClient _httpClient;
        private readonly ISecureStorage _secureStorage;
        private readonly JwtSecurityTokenHandler _jwtHandler = new();

        private string? _accessToken;
        private string? _refreshToken;
        private JwtSecurityToken? _decodedToken;

        public bool IsAuthenticated => this._decodedToken != null && this._decodedToken.ValidTo > DateTime.UtcNow;

        public string? UserEmail => this._decodedToken?.Claims.FirstOrDefault(c => c.Type == emailClaim)?.Value;

        public string? UserFullName => this._decodedToken?.Claims.FirstOrDefault(c => c.Type == nameClaim)?.Value;

        public IReadOnlyList<string> UserRoles => ExtractRoles();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Increase redability of Linq ")]
        private List<string> ExtractRoles()
        {
            if (this._decodedToken == null)
            {
                return [];
            }

            return this._decodedToken.Claims
                                .Where(c => c.Type == roleClaim)
                                .Select(c => c.Value)
                                .ToList();
        }

        public event EventHandler? AuthStateChanged;

        public AuthService(HttpClient httpClient, ISecureStorage secureStorage)
        {
            this._httpClient = httpClient;
            this._secureStorage = secureStorage;

            // Load tokens on startup
            this.LoadStoredTokensAsync().Wait();
        }

        /// <summary>
        /// Executes login.
        /// </summary>
        /// <param name="email">The email.</param>
        /// <param name="password">The password.</param>
        public Task<bool> LoginAsync(string email, string password)
        {
            return this.LoginAsync(email, password, rememberMe: false);
        }

        public async Task<bool> LoginAsync(string email, string password, bool rememberMe)
        {
            try
            {
                LoginRequest request = new LoginRequest { Email = email, Password = password };
                HttpResponseMessage response = await this._httpClient.PostAsJsonAsync("/api/Auth/login", request);

                if (!response.IsSuccessStatusCode)
                    return false;

                LoginResponse? authResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (authResponse == null)
                    return false;

                if(!rememberMe)
                {
                    // If not remembering, clear any stored tokens
                    await this.ClearTokensAsync();
                    await this.StoreTokensAsync(authResponse.AccessToken, authResponse.RefreshToken);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Executes logout.
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                // Try to notify server (optional, may fail if offline)
                if (!string.IsNullOrEmpty(this._accessToken))
                {
                    this._httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this._accessToken);
                    await this._httpClient.PostAsync("/api/Auth/logout", null);
                }
            }
            catch
            {
                // Ignore errors during logout
            }
            finally
            {
                await this.ClearTokensAsync();
            }
        }

        /// <summary>
        /// Executes refresh Token.
        /// </summary>
        public async Task<bool> RefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(this._refreshToken))
                return false;

            try
            {
                RefreshTokenRequest request = new RefreshTokenRequest { RefreshToken = this._refreshToken };
                HttpResponseMessage response = await this._httpClient.PostAsJsonAsync("/api/Auth/refresh", request);

                if (!response.IsSuccessStatusCode)
                {
                    await this.ClearTokensAsync();
                    return false;
                }

                LoginResponse? authResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (authResponse == null)
                    return false;

                await this.StoreTokensAsync(authResponse.AccessToken, authResponse.RefreshToken);
                return true;
            }
            catch
            {
                await this.ClearTokensAsync();
                return false;
            }
        }

        /// <summary>
        /// Gets the access Token.
        /// </summary>
        public async Task<string?> GetAccessTokenAsync()
        {
            // Check if token is still valid
            if (this._decodedToken != null && this._decodedToken.ValidTo > DateTime.UtcNow.AddMinutes(1))
            {
                return this._accessToken;
            }

            // Try to refresh
            if (await this.RefreshTokenAsync())
            {
                return this._accessToken;
            }

            return null;
        }

        private async Task StoreTokensAsync(string accessToken, string refreshToken)
        {
            this._accessToken = accessToken;
            this._refreshToken = refreshToken;
            this._decodedToken = this._jwtHandler.ReadJwtToken(accessToken);

            await this._secureStorage.SetAsync(AccessTokenKey, accessToken);
            await this._secureStorage.SetAsync(RefreshTokenKey, refreshToken);

            this.AuthStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task ClearTokensAsync()
        {
            this._accessToken = null;
            this._refreshToken = null;
            this._decodedToken = null;

            await this._secureStorage.RemoveAsync(AccessTokenKey);
            await this._secureStorage.RemoveAsync(RefreshTokenKey);

            this.AuthStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task LoadStoredTokensAsync()
        {
            try
            {
                this._accessToken = await this._secureStorage.GetAsync(AccessTokenKey);
                this._refreshToken = await this._secureStorage.GetAsync(RefreshTokenKey);

                if (!string.IsNullOrEmpty(this._accessToken))
                {
                    this._decodedToken = this._jwtHandler.ReadJwtToken(this._accessToken);

                    // If token is expired, try to refresh
                    if (this._decodedToken.ValidTo <= DateTime.UtcNow)
                    {
                        await this.RefreshTokenAsync();
                    }
                }
            }
            catch
            {
                // Failed to load tokens, clear everything
                await this.ClearTokensAsync();
            }
        }
    }

}
