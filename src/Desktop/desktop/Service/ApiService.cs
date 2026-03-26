using de.openelp.feuerwehr.domain;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.desktop.Service
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private static string apiUrl = "api/InventoryItem";

        public ApiService(HttpClient http)
        {
            _http = http;
            _http.BaseAddress = new Uri("https://localhost:8081");
        }

        public List<InventoryItem> GetAll()
        {
            var items = _http.GetFromJsonAsync<List<InventoryItem>>(apiUrl).Result;
            return items;
        }

        public async Task Create(InventoryItem item)
        {
            await _http.PostAsJsonAsync(apiUrl, item);
        }
    }
}
