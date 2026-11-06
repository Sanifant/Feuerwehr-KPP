namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    /// <summary>
    /// Carries the result of command envelope operations.
    /// </summary>
    public class CommandEnvelopeResponse
    {
        /// <summary>
        /// Gets or sets the HTTP status code of the response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the serialized event payload.
        /// </summary>
        public string JsonPayload { get; set; } = "{}";
    }
}
