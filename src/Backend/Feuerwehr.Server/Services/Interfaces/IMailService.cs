namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the mail operations required by application consumers.
    /// </summary>
    public interface IMailService
    {
        /// <summary>
        /// Sends the supplied email notification through the configured SMTP server.
        /// </summary>
        /// <param name="recipient">The destination email address.</param>
        /// <param name="subject">The email subject line.</param>
        /// <param name="body">The plain-text email body.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task SendAsync(string recipient, string subject, string body);
    }
}
