using System.Text.Json;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    public static class ResponseHelpers
    {
        /// <summary>
        /// Builds the envelope.
        /// </summary>
        /// <param name="statusCode">The status Code.</param>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
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
