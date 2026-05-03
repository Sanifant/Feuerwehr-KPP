using de.openelp.feuerwehr.domain;
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

        public async Task<LoginResponse?> Login(string username, string password)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(LoginEndpoint, new LoginRequest(username, password));

                if (!response.IsSuccessStatusCode)
                {
                    var reason = await response.Content.ReadAsStringAsync();

                    return new LoginResponse(false, reason, new ApplicationUser());
                }

                var result = await response.Content.ReadFromJsonAsync<ApplicationUser>(JsonOptions);
                return string.IsNullOrWhiteSpace(result?.AccessToken) ?
                    new LoginResponse(false, "Keinen Access Token empfangen", new ApplicationUser()) :
                    new LoginResponse(true, "", result);
            }
            catch (HttpRequestException httpEx)
            {
                return null;
            }
            catch (JsonException jsonEx)
            {
                return null;
            }
            catch (NotSupportedException notSupportedEx)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private sealed record LoginRequest(string Username, string Password);

        public sealed record LoginResponse(bool Success, string ErrorMessage, ApplicationUser User);
    }
}
