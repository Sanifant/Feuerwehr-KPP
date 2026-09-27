using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Feuerwehr.Server.Hubs;

public interface IIncidentClient
{
    Task IncidentUpdated(JsonElement payload);
    Task MapUpdated(JsonElement payload);
    Task DiaryUpdated(JsonElement payload);
}

[Authorize]
public sealed class IncidentHub : Hub<IIncidentClient>
{
    public async Task SubscribeIncident(string incidentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"incident:{incidentId}");
    }

    public async Task SubscribeMap(string incidentId)
    {
        if (!CanViewMap())
        {
            throw new HubException("Keine Berechtigung für Lagekarte.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"map:{incidentId}");
    }

    public async Task SubscribeDiary(string incidentId)
    {
        if (!CanViewDiary())
        {
            throw new HubException("Keine Berechtigung für Einsatztagebuch.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"diary:{incidentId}");
    }

    private bool CanViewMap()
    {
        var roles = Context.User?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        return roles.Contains("SituationMapViewer") || roles.Contains("SituationMapEditor");
    }

    private bool CanViewDiary()
    {
        var roles = Context.User?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        return roles.Contains("IncidentDiaryViewer") || roles.Contains("IncidentDiaryEditor");
    }
}
