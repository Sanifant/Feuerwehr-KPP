using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.Common.Models;

namespace De.Openelp.Feuerwehr.App.Services
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
        /// <summary>
        /// Gets the all.
        /// </summary>
        public async Task<IEnumerable<Hydrant>> GetAllAsync()
        {
            HttpResponseMessage response = await _httpClient.GetAsync(BaseUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IEnumerable<Hydrant>>()
                       ?? new List<Hydrant>();
            }

            return new List<Hydrant>();
        }

        // GET: api/hydrant/5
        /// <summary>
        /// Gets the by Id.
        /// </summary>
        /// <param name="id">The id.</param>
        public async Task<Hydrant> GetByIdAsync(int id)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{BaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Hydrant>();
            }

            return null; // Oder wirf hier eine Exception, je nach gewünschtem Fehlerhandling
        }

        // POST: api/hydrant
        /// <summary>
        /// Creates the operation.
        /// </summary>
        /// <param name="hydrant">The hydrant.</param>
        public async Task<Hydrant> CreateAsync(Hydrant hydrant)
        {
            HttpResponseMessage response = await _httpClient.PostAsync(BaseUrl, JsonContent.Create(hydrant));

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Hydrant>();
            }

            return null;
        }

        // PUT: api/hydrant/5
        /// <summary>
        /// Updates the operation.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="hydrant">The hydrant.</param>
        public async Task<bool> UpdateAsync(int id, Hydrant hydrant)
        {
            HttpResponseMessage response = await _httpClient.PutAsync($"{BaseUrl}/{id}", JsonContent.Create(hydrant));
            return response.IsSuccessStatusCode;
        }

        // DELETE: api/hydrant/5
        /// <summary>
        /// Deletes the operation.
        /// </summary>
        /// <param name="id">The id.</param>
        public async Task<bool> DeleteAsync(int id)
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}

