using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace De.Openelp.Feuerwehr.Server.Hubs;

[Authorize]
public sealed class IncidentHub : Hub<IIncidentClient>
{
    /// <summary>
    /// Executes subscribe Incident.
    /// </summary>
    /// <param name="incidentId">The incident Id.</param>
    public async Task SubscribeIncident(string incidentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"incident:{incidentId}");
    }

    /// <summary>
    /// Executes subscribe Map.
    /// </summary>
    /// <param name="incidentId">The incident Id.</param>
    public async Task SubscribeMap(string incidentId)
    {
        if (!CanViewMap())
        {
            throw new HubException("Keine Berechtigung fÃ¼r Lagekarte.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"map:{incidentId}");
    }

    /// <summary>
    /// Executes subscribe Diary.
    /// </summary>
    /// <param name="incidentId">The incident Id.</param>
    public async Task SubscribeDiary(string incidentId)
    {
        if (!CanViewDiary())
        {
            throw new HubException("Keine Berechtigung fÃ¼r Einsatztagebuch.");
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
