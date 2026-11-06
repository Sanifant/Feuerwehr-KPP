using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.App.Services
{
    // This client deliberately exposes only the two read endpoints, with no lease or mutation calls.
    /// <summary>
    /// Implements situation map operations for the Avalonia client.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to call the backend API.</param>
    public sealed class SituationMapService(AuthenticatedHttpClient httpClient) : ISituationMapService
    {
        private static readonly JsonSerializerOptions JsonOptions = new (JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Retrieves the current active incident, when one exists.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the active incident summary, or null when there is no active incident.</returns>
        public async Task<IncidentSummaryDto?> GetActiveIncidentAsync(CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await httpClient.GetAsync("/api/incidents/active", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            _ = response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IncidentSummaryDto>(JsonOptions, cancellationToken)
                ?? throw new JsonException("Die Einsatzantwort ist leer.");
        }

        /// <summary>
        /// Retrieves the incident's map elements, hydrants, symbols, and current editing lease.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the incident's map elements, hydrants, and current editing lease.</returns>
        public async Task<MapStateResponse> GetStateAsync(Guid incidentId, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await httpClient.GetAsync($"/api/incidents/{incidentId}/map/state", cancellationToken);
            _ = response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MapStateResponse>(JsonOptions, cancellationToken)
                ?? throw new JsonException("Die Lagekartenantwort ist leer.");
        }
    }
}
