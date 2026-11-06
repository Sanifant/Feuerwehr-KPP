namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the html mail operations required by application consumers.
    /// </summary>
    public interface IHtmlMailService
    {
        /// <summary>
        /// Sends a multipart email with plain-text and HTML bodies and the embedded application logo.
        /// </summary>
        /// <param name="recipient">The destination email address.</param>
        /// <param name="subject">The email subject line.</param>
        /// <param name="textBody">The plain-text version of the email body.</param>
        /// <param name="htmlBody">The HTML version of the email body.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task SendHtmlAsync(string recipient, string subject, string textBody, string htmlBody);
    }
}
