using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace De.Openelp.Feuerwehr.Server.Hubs;

public interface IIncidentClient
{
    Task IncidentUpdated(JsonElement payload);
    Task MapUpdated(JsonElement payload);
    Task DiaryUpdated(JsonElement payload);
}
