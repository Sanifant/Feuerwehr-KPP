using System.Text.Json;

namespace De.Openelp.Feuerwehr.Server.Hubs
{
    /// <summary>
    /// Defines the server-to-client SignalR notifications for incident modules.
    /// </summary>
    public interface IIncidentClient
    {
        /// <summary>
        /// Receives a live notification that an incident has changed.
        /// </summary>
        /// <param name="payload">The command or event data to serialize.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task IncidentUpdated(JsonElement payload);

        /// <summary>
        /// Receives a live notification that a situation map has changed.
        /// </summary>
        /// <param name="payload">The command or event data to serialize.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task MapUpdated(JsonElement payload);

        /// <summary>
        /// Receives a live notification that an incident diary has changed.
        /// </summary>
        /// <param name="payload">The command or event data to serialize.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task DiaryUpdated(JsonElement payload);
    }
}
