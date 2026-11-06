using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace De.Openelp.Feuerwehr.Server.Hubs
{
    [Authorize]
    public sealed class IncidentHub : Hub<IIncidentClient>
    {
        /// <summary>
        /// Executes subscribe Incident.
        /// </summary>
        /// <param name="incidentId">The incident Id.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task SubscribeIncident(string incidentId)
        {
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"incident:{incidentId}");
        }

        /// <summary>
        /// Executes subscribe Map.
        /// </summary>
        /// <param name="incidentId">The incident Id.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task SubscribeMap(string incidentId)
        {
            if (!this.CanViewMap())
            {
                throw new HubException("Keine Berechtigung für Lagekarte.");
            }

            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"map:{incidentId}");
        }

        /// <summary>
        /// Executes subscribe Diary.
        /// </summary>
        /// <param name="incidentId">The incident Id.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
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
