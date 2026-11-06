namespace de.openelp.feuerwehr.Server.Services
{
    public interface IMailService
    {
        Task SendAsync(string recipient, string subject, string body);
    }
}
