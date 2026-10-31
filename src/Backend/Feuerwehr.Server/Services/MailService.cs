using MailKit.Net.Smtp;
using MimeKit;

namespace De.Openelp.Feuerwehr.Server.Services
{

    /// <summary>
    /// Executes mail Service.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="logger">The logger.</param>
    public sealed class MailService(ISmtpClient client, SmtpSettings settings, ILogger<MailService> logger) : IMailService, IHtmlMailService
    {
        /// <summary>
        /// Sends the operation.
        /// </summary>
        /// <param name="recipient">The recipient.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="body">The body.</param>
        public Task SendAsync(string recipient, string subject, string body) =>
            SendMessageAsync(recipient, subject, new TextPart("plain") { Text = body });

        /// <summary>
        /// Sends the html.
        /// </summary>
        /// <param name="recipient">The recipient.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="textBody">The text Body.</param>
        /// <param name="htmlBody">The html Body.</param>
        public async Task SendHtmlAsync(string recipient, string subject, string textBody, string htmlBody)
        {
            var builder = new BodyBuilder { TextBody = textBody, HtmlBody = htmlBody };
            using var logoStream = typeof(MailService).Assembly.GetManifestResourceStream("De.Openelp.Feuerwehr.Server.MailLogo.png")
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
