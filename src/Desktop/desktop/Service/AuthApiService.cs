using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.desktop.Service
{
    public class AuthApiService
    {
        private readonly HttpClient _http;
        private const string LoginEndpoint = "api/auth/login";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AuthApiService(HttpClient http, IOptions<ApiSettings> settings)
        {
            _http = http;
            _http.BaseAddress = new Uri(settings.Value.AuthBaseUrl);
        }

        public async Task<string?> Login(string username, string password)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(LoginEndpoint, new LoginRequest(username, password));

                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
                return string.IsNullOrWhiteSpace(result?.AccessToken) ? null : result.AccessToken;
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        private sealed record LoginRequest(string Username, string Password);

        private sealed class LoginResponse
        {
            [JsonPropertyName("accessToken")]
            public string AccessToken { get; set; } = string.Empty;

            [JsonPropertyName("refreshToken")]
            public string RefreshToken { get; set; } = string.Empty;
        }
    }
}
