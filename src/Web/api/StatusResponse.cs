namespace de.openelp.feuerwehr.Api
{
    public class StatusResponse
    {
        public required string Status { get; set; }
        public required string Database { get; set; }
        public required string Redis { get; set; }
    }
}
