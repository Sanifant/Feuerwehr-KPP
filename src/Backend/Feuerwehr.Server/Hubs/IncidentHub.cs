using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace De.Openelp.Feuerwehr.Server.Hubs
{
    /// <summary>
    /// Authorizes SignalR subscriptions for incident, diary, and situation-map updates.
    /// </summary>
    [Authorize]
    public sealed class IncidentHub : Hub<IIncidentClient>
    {
        /// <summary>
        /// Adds an authorized connection to the incident's live update group.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task SubscribeIncident(string incidentId)
        {
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"incident:{incidentId}");
        }

        /// <summary>
        /// Adds an authorized connection to the situation-map update group.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task SubscribeMap(string incidentId)
        {
            if (!this.CanViewMap())
            {
                throw new HubException("Keine Berechtigung für Lagekarte.");
            }

            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"map:{incidentId}");
        }

        /// <summary>
        /// Adds an authorized connection to the incident diary update group.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task SubscribeDiary(string incidentId)
        {
            if (!this.CanViewDiary())
            {
                throw new HubException("Keine Berechtigung für Einsatztagebuch.");
            }

            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"diary:{incidentId}");
        }

        private bool CanViewMap()
        {
            HashSet<string> roles = this.Context.User?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
            return roles.Contains("SituationMapViewer") || roles.Contains("SituationMapEditor");
        }

        private bool CanViewDiary()
        {
            HashSet<string> roles = this.Context.User?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
            return roles.Contains("IncidentDiaryViewer") || roles.Contains("IncidentDiaryEditor");
        }
    }
}
