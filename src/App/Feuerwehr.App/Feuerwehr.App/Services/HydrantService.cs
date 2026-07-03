using Feuerwehr.Common.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Feuerwehr.App.Services
{
    public class HydrantService : IHydrantService
    {
        private readonly AuthenticatedHttpClient _httpClient;
        private const string BaseUrl = "api/hydrant";

        public HydrantService(AuthenticatedHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // GET: api/hydrant
        public async Task<IEnumerable<Hydrant>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync(BaseUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IEnumerable<Hydrant>>() 
                       ?? new List<Hydrant>();
            }

            return new List<Hydrant>();
        }

        // GET: api/hydrant/5
        public async Task<Hydrant> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Hydrant>();
            }

            return null; // Oder wirf hier eine Exception, je nach gewünschtem Fehlerhandling
        }

        // POST: api/hydrant
        public async Task<Hydrant> CreateAsync(Hydrant hydrant)
        {
            var response = await _httpClient.PostAsync(BaseUrl, JsonContent.Create(hydrant));

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Hydrant>();
            }

            return null;
        }

        // PUT: api/hydrant/5
        public async Task<bool> UpdateAsync(int id, Hydrant hydrant)
        {
            var response = await _httpClient.PutAsync($"{BaseUrl}/{id}", JsonContent.Create(hydrant));
            return response.IsSuccessStatusCode;
        }

        // DELETE: api/hydrant/5
        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
