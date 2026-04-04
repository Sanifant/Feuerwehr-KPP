using de.openelp.feuerwehr.domain;
using de.openelp.feuerwehr.desktop.ViewModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.desktop.Service
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly AuthTokenStore _tokenStore;
        private const string ApiUrl = "api/InventoryItem";

        public ApiService(HttpClient http, AuthTokenStore tokenStore, IOptions<ApiSettings> settings)
        {
            _http = http;
            _tokenStore = tokenStore;
            _http.BaseAddress = new Uri(settings.Value.BaseUrl);
        }

        private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(_tokenStore.Token))
            {
                throw new InvalidOperationException("Authentication token is required for API requests.");
            }

            var request = new HttpRequestMessage(method, relativeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.Token);
            return request;
        }

        public List<InventoryItem> GetAll()
        {
            using var request = CreateAuthorizedRequest(HttpMethod.Get, ApiUrl);
            using var response = _http.Send(request);
            response.EnsureSuccessStatusCode();

            var items = response.Content.ReadFromJsonAsync<List<InventoryItem>>().GetAwaiter().GetResult();
            return items ?? new List<InventoryItem>();
        }

        public async Task Create(InventoryItem item)
        {
            using var request = CreateAuthorizedRequest(HttpMethod.Post, ApiUrl);
            request.Content = JsonContent.Create(item);

            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}
