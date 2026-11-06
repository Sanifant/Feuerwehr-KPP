using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.Common.Models.Auth;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Maintains the client's authenticated session and stores tokens through secure storage.
    /// </summary>
    public class AuthService : IAuthService
    {
        private const string AccessTokenKey = "access_token";
        private const string RefreshTokenKey = "refresh_token";

        private const string EmailClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

        private const string NameClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

        private const string RoleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

        private readonly HttpClient _httpClient;
        private readonly ISecureStorage _secureStorage;
        private readonly JwtSecurityTokenHandler _jwtHandler = new ();

        private string? _accessToken;
        private string? _refreshToken;
        private JwtSecurityToken? _decodedToken;

        /// <summary>
        /// Gets a value indicating whether a user is currently signed in.
        /// </summary>
        public bool IsAuthenticated => this._decodedToken != null && this._decodedToken.ValidTo > DateTime.UtcNow;

        /// <summary>
        /// Gets the signed-in user's email address.
        /// </summary>
        public string? UserEmail => this._decodedToken?.Claims.FirstOrDefault(c => c.Type == EmailClaim)?.Value;

        /// <summary>
        /// Gets the signed-in user's full display name.
        /// </summary>
        public string? UserFullName => this._decodedToken?.Claims.FirstOrDefault(c => c.Type == NameClaim)?.Value;

        /// <summary>
        /// Gets the roles granted to the signed-in user.
        /// </summary>
        public IReadOnlyList<string> UserRoles
        {
            get
            {
                if (this._decodedToken == null)
                {
                    return Array.Empty<string>();
                }

                return this._decodedToken.Claims
                    .Where(c => c.Type == RoleClaim)
                    .Select(c => c.Value)
                    .ToList();
            }
        }

        /// <summary>
        /// Occurs when the authentication state changes so subscribed views can refresh their permissions.
        /// </summary>
        public event EventHandler? AuthStateChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthService" /> class with its required dependencies.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to call the backend API.</param>
        /// <param name="secureStorage">The platform storage used to retain authentication credentials.</param>
        public AuthService(HttpClient httpClient, ISecureStorage secureStorage)
        {
            this._httpClient = httpClient;
            this._secureStorage = secureStorage;

            // Load tokens on startup
            _ = this.LoadStoredTokensAsync();
        }

        /// <summary>
        /// Authenticates the supplied email and password and establishes a session subject to account activation and lockout checks.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="password">The password supplied for authentication.</param>
        /// <returns>A task whose result is true if authentication succeeds; otherwise, false.</returns>
        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                LoginRequest request = new ()
                { Email = email, Password = password };
                HttpResponseMessage response = await this._httpClient.PostAsJsonAsync("/api/Auth/login", request);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                LoginResponse? authResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (authResponse == null)
                {
                    return false;
                }

                await this.StoreTokensAsync(authResponse.AccessToken, authResponse.RefreshToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Revokes the current session's refresh token and signs the user out.
        /// </summary>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task LogoutAsync()
        {
            try
            {
                // Try to notify server (optional, may fail if offline)
                if (!string.IsNullOrEmpty(this._accessToken))
                {
                    this._httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this._accessToken);
                    _ = await this._httpClient.PostAsync("/api/Auth/logout", null);
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
        /// Renews the authenticated session after validating the refresh token and account state.
        /// </summary>
        /// <returns>A task whose result is true if the session is renewed; otherwise, false.</returns>
        public async Task<bool> RefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(this._refreshToken))
            {
                return false;
            }

            try
            {
                RefreshTokenRequest request = new ()
                { RefreshToken = this._refreshToken };
                HttpResponseMessage response = await this._httpClient.PostAsJsonAsync("/api/Auth/refresh", request);

                if (!response.IsSuccessStatusCode)
                {
                    await this.ClearTokensAsync();
                    return false;
                }

                LoginResponse? authResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (authResponse == null)
                {
                    return false;
                }

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
        /// Retrieves the access token for authenticated API calls, renewing the session when needed.
        /// </summary>
        /// <returns>A task whose result is the current access token, or null if no valid authenticated session is available.</returns>
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
                        _ = await this.RefreshTokenAsync();
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
