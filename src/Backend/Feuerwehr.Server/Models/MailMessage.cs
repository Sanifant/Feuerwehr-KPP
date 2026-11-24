namespace de.openelp.feuerwehr.Server.Models
{
    /// <summary>A durable outgoing email, independent of its originating module.</summary>
    public sealed class MailMessage
    {
        public Guid Id { get; set; }

        public string Source { get; set; } = string.Empty;

        public string? DeduplicationKey { get; set; }

        public string Recipient { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string TextBody { get; set; } = string.Empty;

        public string? HtmlBody { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime NextAttemptAtUtc { get; set; }

        public Guid? LeaseId { get; set; }

        public DateTime? LeaseUntilUtc { get; set; }

        public DateTime? SentAtUtc { get; set; }

        public int Attempts { get; set; }

        public string? LastError { get; set; }

        public string? ModifiedBy { get; set; }

        public ApplicationUser? ModifiedByUser { get; set; }

        public DateTime ModifiedOn { get; set; }
    }
}
