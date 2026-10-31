using MailKit.Net.Smtp;
using MimeKit;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IMailService
    {
        Task SendAsync(string recipient, string subject, string body);
    }
}
