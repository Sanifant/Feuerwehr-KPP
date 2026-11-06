namespace de.openelp.feuerwehr.Server.Services
{
    public interface IHtmlMailService
    {
        Task SendHtmlAsync(string recipient, string subject, string textBody, string htmlBody);
    }
}
