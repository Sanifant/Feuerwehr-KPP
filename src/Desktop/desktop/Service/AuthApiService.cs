using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.desktop.Service
{
    public class AuthApiService
    {
        private readonly HttpClient _http;

        public AuthApiService(HttpClient http, IOptions<ApiSettings> settings)
        {
            _http = http;
            _http.BaseAddress = new Uri(settings.Value.AuthBaseUrl);
        }

        public async Task<string?> Login(string username, string password)
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", new
            {
                username,
                password
            });

            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return result?.Token;
        }

        private class LoginResponse
        {
            public string Token { get; set; }
        }
    }
}
