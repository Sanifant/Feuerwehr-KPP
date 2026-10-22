using MailKit.Net.Smtp;
using MimeKit;

namespace de.openelp.feuerwehr.Server.Services
{
    public interface IMailService
    {
        Task SendAsync(string recipient, string subject, string body);
    }

    public sealed class MailService(ISmtpClient client, SmtpSettings settings, ILogger<MailService> logger) : IMailService
    {
        public async Task SendAsync(string recipient, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.FromName, settings.From));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };
            client.Timeout = settings.TimeoutMilliseconds;
            try
            {
                await client.ConnectAsync(settings.Host, settings.Port, settings.Security);
                if (settings.Username is not null)
                    await client.AuthenticateAsync(settings.Username, settings.Password!);
                await client.SendAsync(message);
            }
            finally
            {
                if (client.IsConnected)
                {
                    try { await client.DisconnectAsync(true); }
                    catch (Exception exception)
                    {
                        // A cleanup error must not roll back an invitation whose email was already accepted.
                        logger.LogWarning(exception, "SMTP-Verbindung konnte nicht sauber geschlossen werden.");
                    }
                }
            }
        }
    }
}
