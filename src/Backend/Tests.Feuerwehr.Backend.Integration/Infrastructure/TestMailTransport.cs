namespace de.openelp.feuerwehr.Tests.Backend.Integration.Infrastructure
{
    using System.Collections.Concurrent;
    using de.openelp.feuerwehr.Server.Services;

    /// <summary>Records deliveries and allows tests to pause or fail SMTP acceptance.</summary>
    public sealed class TestMailTransport : IMailService, IHtmlMailService
    {
        public ConcurrentBag<string> TextRecipients { get; } = new ConcurrentBag<string>();

        public ConcurrentBag<string> HtmlRecipients { get; } = new ConcurrentBag<string>();

        public Func<string, Task> Send { get; set; } = _ => Task.CompletedTask;

        public Task SendAsync(string recipient, string subject, string body)
        {
            this.TextRecipients.Add(recipient);
            return this.Send(recipient);
        }

        public Task SendHtmlAsync(string recipient, string subject, string textBody, string htmlBody)
        {
            this.HtmlRecipients.Add(recipient);
            return this.Send(recipient);
        }
    }
}
