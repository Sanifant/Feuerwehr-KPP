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
    /// Executes situation Map Service.
    /// </summary>
    /// <param name="httpClient">The http Client.</param>
    public sealed class SituationMapService(AuthenticatedHttpClient httpClient) : ISituationMapService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Gets the active Incident.
        /// </summary>
        /// <param name="cancellationToken">The cancellation Token.</param>
        public async Task<IncidentSummaryDto?> GetActiveIncidentAsync(CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await httpClient.GetAsync("/api/incidents/active", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IncidentSummaryDto>(JsonOptions, cancellationToken)
                ?? throw new JsonException("Die Einsatzantwort ist leer.");
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <param name="incidentId">The incident Id.</param>
        /// <param name="cancellationToken">The cancellation Token.</param>
        public async Task<MapStateResponse> GetStateAsync(Guid incidentId, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await httpClient.GetAsync($"/api/incidents/{incidentId}/map/state", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MapStateResponse>(JsonOptions, cancellationToken)
                ?? throw new JsonException("Die Lagekartenantwort ist leer.");
        }
    }

}
