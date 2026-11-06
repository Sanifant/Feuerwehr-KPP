using System.Text.Json;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    public static class JsonOptionsFactory
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }
}
