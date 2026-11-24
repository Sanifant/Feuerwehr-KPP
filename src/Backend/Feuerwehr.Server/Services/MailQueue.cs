namespace de.openelp.feuerwehr.Server.Services
{
    using de.openelp.feuerwehr.Server.Data;
    using de.openelp.feuerwehr.Server.Models;

    /// <summary>Stores all modules' outgoing messages in the same queue.</summary>
    public sealed class MailQueue(FeuerwehrDbContext db, TimeProvider clock) : IMailQueue
    {
        /// <inheritdoc />
        public void Enqueue(MailMessage message, string? createdBy)
        {
            DateTime now = clock.GetUtcNow().UtcDateTime;
            message.Id = Guid.NewGuid();
            message.CreatedAtUtc = now;
            message.NextAttemptAtUtc = now;
            message.ModifiedBy = createdBy;
            message.ModifiedOn = now;
            db.MailMessages.Add(message);
        }
    }
}
