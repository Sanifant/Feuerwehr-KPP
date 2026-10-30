using MailKit.Net.Smtp;
using MimeKit;

namespace de.openelp.feuerwehr.Server.Services
{
    public interface IMailService
    {
        Task SendAsync(string recipient, string subject, string body);
    }

    public interface IHtmlMailService
    {
        Task SendHtmlAsync(string recipient, string subject, string textBody, string htmlBody);
    }

    public sealed class MailService(ISmtpClient client, SmtpSettings settings, ILogger<MailService> logger) : IMailService, IHtmlMailService
    {
        public Task SendAsync(string recipient, string subject, string body) =>
            SendMessageAsync(recipient, subject, new TextPart("plain") { Text = body });

        public async Task SendHtmlAsync(string recipient, string subject, string textBody, string htmlBody)
        {
            var builder = new BodyBuilder { TextBody = textBody, HtmlBody = htmlBody };
            using var logoStream = typeof(MailService).Assembly.GetManifestResourceStream("de.openelp.feuerwehr.Server.MailLogo.png")
                ?? throw new InvalidOperationException("Das eingebettete E-Mail-Logo fehlt.");
            var logo = builder.LinkedResources.Add("feuerwehr.png", logoStream, new ContentType("image", "png"));
            logo.ContentId = "feuerwehr-logo";
            await SendMessageAsync(recipient, subject, builder.ToMessageBody());
        }

        private async Task SendMessageAsync(string recipient, string subject, MimeEntity body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.FromName, settings.From));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;
            message.Body = body;
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
