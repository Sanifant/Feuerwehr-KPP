using MailKit.Net.Smtp;
using MimeKit;

namespace De.Openelp.Feuerwehr.Server.Services
{

    /// <summary>
    /// Implements mail operations for the API application layer.
    /// </summary>
    /// <param name="client">The authenticated HTTP client used by the test scenario.</param>
    /// <param name="settings">The resolved configuration settings used by this component.</param>
    /// <param name="logger">The logger used to record operational failures and diagnostics.</param>
    public sealed class MailService(ISmtpClient client, SmtpSettings settings, ILogger<MailService> logger) : IMailService, IHtmlMailService
    {
        /// <summary>
        /// Sends the supplied email notification through the configured SMTP server.
        /// </summary>
        /// <param name="recipient">The destination email address.</param>
        /// <param name="subject">The email subject line.</param>
        /// <param name="body">The plain-text email body.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public Task SendAsync(string recipient, string subject, string body) =>
            this.SendMessageAsync(recipient, subject, new TextPart("plain") { Text = body });

        /// <summary>
        /// Sends a multipart email with plain-text and HTML bodies and the embedded application logo.
        /// </summary>
        /// <param name="recipient">The destination email address.</param>
        /// <param name="subject">The email subject line.</param>
        /// <param name="textBody">The plain-text version of the email body.</param>
        /// <param name="htmlBody">The HTML version of the email body.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task SendHtmlAsync(string recipient, string subject, string textBody, string htmlBody)
        {
            BodyBuilder builder = new ()
            { TextBody = textBody, HtmlBody = htmlBody };
            using Stream logoStream = typeof(MailService).Assembly.GetManifestResourceStream("De.Openelp.Feuerwehr.Server.MailLogo.png")
                ?? throw new InvalidOperationException("Das eingebettete E-Mail-Logo fehlt.");
            MimeEntity logo = builder.LinkedResources.Add("feuerwehr.png", logoStream, new ContentType("image", "png"));
            logo.ContentId = "feuerwehr-logo";
            await this.SendMessageAsync(recipient, subject, builder.ToMessageBody());
        }

        private async Task SendMessageAsync(string recipient, string subject, MimeEntity body)
        {
            MimeMessage message = new ();
            message.From.Add(new MailboxAddress(settings.FromName, settings.From));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;
            message.Body = body;
            client.Timeout = settings.TimeoutMilliseconds;
            try
            {
                await client.ConnectAsync(settings.Host, settings.Port, settings.Security);
                if (settings.Username is not null)
                {
                    await client.AuthenticateAsync(settings.Username, settings.Password!);
                }

                _ = await client.SendAsync(message);
            }
            finally
            {
                if (client.IsConnected)
                {
                    try
                    {
                        await client.DisconnectAsync(true);
                    }
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
