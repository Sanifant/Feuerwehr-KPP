using System.Text.Json;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Serializes command payloads into the response envelope stored for idempotent replay.
    /// </summary>
    public static class ResponseHelpers
    {
        /// <summary>
        /// Builds the envelope.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="payload">The command or event data to serialize.</param>
        /// <returns>The status and serialized payload retained for command replay.</returns>
        public static CommandEnvelopeResponse BuildEnvelope(int statusCode, object payload)
        {
            return new CommandEnvelopeResponse
            {
                StatusCode = statusCode,
                JsonPayload = JsonSerializer.Serialize(payload, JsonOptionsFactory.Default),
            };
        }
    }
}
