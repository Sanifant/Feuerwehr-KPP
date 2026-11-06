using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.Common.Models;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Implements hydrant operations for the Avalonia client.
    /// </summary>
    public class HydrantService : IHydrantService
    {
        private readonly AuthenticatedHttpClient _httpClient;
        private const string BaseUrl = "api/hydrant";

        /// <summary>
        /// Initializes a new instance of the <see cref="HydrantService" /> class with its required dependencies.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to call the backend API.</param>
        public HydrantService(AuthenticatedHttpClient httpClient)
        {
            this._httpClient = httpClient;
        }

        // GET: api/hydrant
        /// <summary>
        /// Retrieves hydrant records visible to the caller.
        /// </summary>
        /// <returns>A task whose result is the hydrants returned by the API.</returns>
        public async Task<IEnumerable<Hydrant>> GetAllAsync()
        {
            HttpResponseMessage response = await this._httpClient.GetAsync(BaseUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IEnumerable<Hydrant>>()
                       ?? new List<Hydrant>();
            }

            return new List<Hydrant>();
        }

        // GET: api/hydrant/5
        /// <summary>
        /// Retrieves hydrant records matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to retrieve.</param>
        /// <returns>A task whose result is the hydrant returned by the API.</returns>
        public async Task<Hydrant> GetByIdAsync(int id)
        {
            HttpResponseMessage response = await this._httpClient.GetAsync($"{BaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Hydrant>();
            }

            return null; // Oder wirf hier eine Exception, je nach gewünschtem Fehlerhandling
        }

        // POST: api/hydrant
        /// <summary>
        /// Creates hydrant records from the validated input.
        /// </summary>
        /// <param name="hydrant">The hydrant data to create or update.</param>
        /// <returns>A task whose result is the hydrant returned by the API.</returns>
        public async Task<Hydrant> CreateAsync(Hydrant hydrant)
        {
            HttpResponseMessage response = await this._httpClient.PostAsync(BaseUrl, JsonContent.Create(hydrant));

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Hydrant>();
            }

            return null;
        }

        // PUT: api/hydrant/5
        /// <summary>
        /// Updates hydrant records matching the supplied identifier after validating the input.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to process.</param>
        /// <param name="hydrant">The hydrant data to create or update.</param>
        /// <returns>A task whose result is true if the requested hydrant operation succeeds; otherwise, false.</returns>
        public async Task<bool> UpdateAsync(int id, Hydrant hydrant)
        {
            HttpResponseMessage response = await this._httpClient.PutAsync($"{BaseUrl}/{id}", JsonContent.Create(hydrant));
            return response.IsSuccessStatusCode;
        }

        // DELETE: api/hydrant/5
        /// <summary>
        /// Deletes the hydrant record matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to process.</param>
        /// <returns>A task whose result is true if the requested hydrant operation succeeds; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            HttpResponseMessage response = await this._httpClient.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
