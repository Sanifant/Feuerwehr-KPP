using System.Text.Json;

namespace de.openelp.feuerwehr.Server.Hubs
{
    public interface IIncidentClient
    {
        Task IncidentUpdated(JsonElement payload);
        Task MapUpdated(JsonElement payload);
        Task DiaryUpdated(JsonElement payload);
    }
}
