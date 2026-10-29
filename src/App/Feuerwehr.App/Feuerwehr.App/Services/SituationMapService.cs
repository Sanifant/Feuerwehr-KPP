using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Common.Models.IncidentModules;

namespace de.openelp.feuerwehr.App.Services;

// This client deliberately exposes only the two read endpoints, with no lease or mutation calls.
public sealed class SituationMapService(AuthenticatedHttpClient httpClient) : ISituationMapService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IncidentSummaryDto?> GetActiveIncidentAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("/api/incidents/active", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IncidentSummaryDto>(JsonOptions, cancellationToken)
            ?? throw new JsonException("Die Einsatzantwort ist leer.");
    }

    public async Task<MapStateResponse> GetStateAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"/api/incidents/{incidentId}/map/state", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MapStateResponse>(JsonOptions, cancellationToken)
            ?? throw new JsonException("Die Lagekartenantwort ist leer.");
    }
}
