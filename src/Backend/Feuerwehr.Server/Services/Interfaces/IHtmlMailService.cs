using MailKit.Net.Smtp;
using MimeKit;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IHtmlMailService
    {
        Task SendHtmlAsync(string recipient, string subject, string textBody, string htmlBody);
    }
}
