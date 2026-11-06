using System.Text.Json;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Provides the JSON serialization options used for incident payloads and stored command responses.
    /// </summary>
    public static class JsonOptionsFactory
    {
        /// <summary>
        /// The shared JSON serialization options for incident contracts.
        /// </summary>
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }
}
