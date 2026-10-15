namespace de.openelp.feuerwehr.Common.Models.Auth
{
    public class ResetRequest
    {
        public string Token { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;
    }
}