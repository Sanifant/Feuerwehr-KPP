using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Feuerwehr.Common.Models.Auth;

namespace Feuerwehr.App.Services;

public class AuthService : IAuthService
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";

    private readonly HttpClient _httpClient;
    private readonly ISecureStorage _secureStorage;
    private readonly JwtSecurityTokenHandler _jwtHandler = new();

    private string? _accessToken;
    private string? _refreshToken;
    private JwtSecurityToken? _decodedToken;

    public bool IsAuthenticated => _decodedToken != null && _decodedToken.ValidTo > DateTime.UtcNow;

    public string? UserEmail => _decodedToken?.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

    public string? UserFullName => _decodedToken?.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

    public IReadOnlyList<string> UserRoles
    {
        get
        {
            if (_decodedToken == null) return Array.Empty<string>();
            return _decodedToken.Claims
                .Where(c => c.Type == "role")
                .Select(c => c.Value)
                .ToList();
        }
    }

    public event EventHandler? AuthStateChanged;

    public AuthService(HttpClient httpClient, ISecureStorage secureStorage)
    {
        _httpClient = httpClient;
        _secureStorage = secureStorage;

        // Load tokens on startup
        _ = LoadStoredTokensAsync();
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var request = new LoginRequest { Email = email, Password = password };
            var response = await _httpClient.PostAsJsonAsync("/api/Auth/login", request);

            if (!response.IsSuccessStatusCode)
                return false;

            var authResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (authResponse == null)
                return false;

            await StoreTokensAsync(authResponse.AccessToken, authResponse.RefreshToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Try to notify server (optional, may fail if offline)
            if (!string.IsNullOrEmpty(_accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                await _httpClient.PostAsync("/api/Auth/logout", null);
            }
        }
        catch
        {
            // Ignore errors during logout
        }
        finally
        {
            await ClearTokensAsync();
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken))
            return false;

        try
        {
            var request = new RefreshTokenRequest { RefreshToken = _refreshToken };
            var response = await _httpClient.PostAsJsonAsync("/api/Auth/refresh", request);

            if (!response.IsSuccessStatusCode)
            {
                await ClearTokensAsync();
                return false;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (authResponse == null)
                return false;

            await StoreTokensAsync(authResponse.AccessToken, authResponse.RefreshToken);
            return true;
        }
        catch
        {
            await ClearTokensAsync();
            return false;
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        // Check if token is still valid
        if (_decodedToken != null && _decodedToken.ValidTo > DateTime.UtcNow.AddMinutes(1))
        {
            return _accessToken;
        }

        // Try to refresh
        if (await RefreshTokenAsync())
        {
            return _accessToken;
        }

        return null;
    }

    private async Task StoreTokensAsync(string accessToken, string refreshToken)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _decodedToken = _jwtHandler.ReadJwtToken(accessToken);

        await _secureStorage.SetAsync(AccessTokenKey, accessToken);
        await _secureStorage.SetAsync(RefreshTokenKey, refreshToken);

        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task ClearTokensAsync()
    {
        _accessToken = null;
        _refreshToken = null;
        _decodedToken = null;

        await _secureStorage.RemoveAsync(AccessTokenKey);
        await _secureStorage.RemoveAsync(RefreshTokenKey);

        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadStoredTokensAsync()
    {
        try
        {
            _accessToken = await _secureStorage.GetAsync(AccessTokenKey);
            _refreshToken = await _secureStorage.GetAsync(RefreshTokenKey);

            if (!string.IsNullOrEmpty(_accessToken))
            {
                _decodedToken = _jwtHandler.ReadJwtToken(_accessToken);

                // If token is expired, try to refresh
                if (_decodedToken.ValidTo <= DateTime.UtcNow)
                {
                    await RefreshTokenAsync();
                }
            }
        }
        catch
        {
            // Failed to load tokens, clear everything
            await ClearTokensAsync();
        }
    }
}
