using System.Net.Mail;
using System.Data.Common;

namespace de.openelp.feuerwehr.Server.Services
{
    public interface IMailService
    {
        Task SendAsync(string recipient, string subject, string body);
    }

    public sealed class MailService(SmtpClient client, IConfiguration configuration) : IMailService
    {
        public static SmtpClient CreateClient(IConfiguration configuration)
        {
            Uri? endpoint = null;
            var connection = configuration.GetConnectionString("mail");
            if (!string.IsNullOrWhiteSpace(connection))
            {
                // Support both Mailpit URI and endpoint=... connection-string formats.
                if (!Uri.TryCreate(connection, UriKind.Absolute, out endpoint) || endpoint.Scheme != "smtp")
                {
                    var builder = new DbConnectionStringBuilder { ConnectionString = connection };
                    endpoint = new Uri((string)builder["endpoint"], UriKind.Absolute);
                }
                if (endpoint.Scheme != "smtp") throw new InvalidOperationException("Ungültiger SMTP-Endpunkt.");
            }

            var host = configuration["MAIL_HOST"] ?? endpoint?.Host ?? "localhost";
            var port = int.TryParse(configuration["MAIL_PORT"], out var configuredPort)
                ? configuredPort : endpoint is { Port: > 0 } ? endpoint.Port : 1025;
            return new SmtpClient(host, port);
        }

        public async Task SendAsync(string recipient, string subject, string body)
        {
            using var message = new MailMessage(configuration["Mail:From"] ?? "noreply@localhost.de", recipient, subject, body);
            await client.SendMailAsync(message);
        }
    }
}
