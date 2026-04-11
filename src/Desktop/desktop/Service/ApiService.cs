using de.openelp.feuerwehr.domain;
using de.openelp.feuerwehr.desktop.ViewModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using de.openelp.feuerwehr.desktop.Interfaces;

namespace de.openelp.feuerwehr.desktop.Service
{
    public class ApiService : IApiService
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
            if (_tokenStore.Token == null || string.IsNullOrWhiteSpace(_tokenStore.Token.AccessToken))
            {
                throw new InvalidOperationException("Authentication token is required for API requests.");
            }

            var request = new HttpRequestMessage(method, relativeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.Token.AccessToken);
            return request;
        }

        public async Task<List<InventoryItem>> GetAll()
        {
            using var request = CreateAuthorizedRequest(HttpMethod.Get, ApiUrl);
            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var items = await response.Content.ReadFromJsonAsync<List<InventoryItem>>();
            return items ?? new List<InventoryItem>();
        }

        public async Task Create(InventoryItem item)
        {
            using var request = CreateAuthorizedRequest(HttpMethod.Post, ApiUrl);
            request.Content = JsonContent.Create(item);

            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public InventoryCategory[] GetInventoryCategories()
        {
            var apiUrl = ApiUrl + "/categories";
            using var request = CreateAuthorizedRequest(HttpMethod.Get, apiUrl);
            using var response = _http.Send(request);
            response.EnsureSuccessStatusCode();

            var categories = response.Content.ReadFromJsonAsync<InventoryCategory[]>().GetAwaiter().GetResult();
            return categories ?? Array.Empty<InventoryCategory>();
        }
    }
}
