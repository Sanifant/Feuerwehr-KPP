namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    public class CommandEnvelopeResponse
    {
        public int StatusCode { get; set; }

        public string JsonPayload { get; set; } = "{}";
    }

}
