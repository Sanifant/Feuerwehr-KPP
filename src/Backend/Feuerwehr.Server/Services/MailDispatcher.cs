namespace de.openelp.feuerwehr.Server.Services
{
    using de.openelp.feuerwehr.Server.Data;
    using de.openelp.feuerwehr.Server.Models;
    using Microsoft.EntityFrameworkCore;

    /// <summary>Sends queued emails using atomic leases and exponential retry delays.</summary>
    public sealed class MailDispatcher(FeuerwehrDbContext db, IMailService textMail, IHtmlMailService htmlMail,
        TimeProvider clock, ILogger<MailDispatcher> logger) : IMailDispatcher
    {
        /// <inheritdoc />
        public async Task DispatchAsync(CancellationToken cancellationToken)
        {
            DateTime now = clock.GetUtcNow().UtcDateTime;
            List<Guid> candidates = await db.MailMessages.AsNoTracking()
                .Where(x => x.SentAtUtc == null && x.NextAttemptAtUtc <= now && (x.LeaseUntilUtc == null || x.LeaseUntilUtc <= now))
                .OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id).Select(x => x.Id).Take(20).ToListAsync(cancellationToken);
            foreach (Guid id in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                now = clock.GetUtcNow().UtcDateTime;
                Guid leaseId = Guid.NewGuid();
                DateTime leaseUntil = now.AddMinutes(5);
                MailMessage message;
                if (db.Database.IsRelational())
                {
                    int claimed = await db.MailMessages.Where(x => x.Id == id && x.SentAtUtc == null && x.NextAttemptAtUtc <= now &&
                        (x.LeaseUntilUtc == null || x.LeaseUntilUtc <= now)).ExecuteUpdateAsync(update => update
                            .SetProperty(x => x.LeaseId, leaseId).SetProperty(x => x.LeaseUntilUtc, leaseUntil)
                            .SetProperty(x => x.Attempts, x => x.Attempts + 1).SetProperty(x => x.ModifiedOn, now), cancellationToken);
                    if (claimed == 0)
                    {
                        continue;
                    }

                    message = await db.MailMessages.AsNoTracking().SingleAsync(x => x.Id == id && x.LeaseId == leaseId, cancellationToken);
                }
                else
                {
                    // The non-relational branch supports isolated InMemory tests; production uses atomic SQL updates.
                    message = await db.MailMessages.SingleAsync(x => x.Id == id, cancellationToken);
                    message.LeaseId = leaseId;
                    message.LeaseUntilUtc = leaseUntil;
                    message.Attempts++;
                    message.ModifiedOn = now;
                    await db.SaveChangesAsync(cancellationToken);
                }

                DateTime? sent = null;
                string? error = null;
                try
                {
                    if (message.HtmlBody is null)
                    {
                        await textMail.SendAsync(message.Recipient, message.Subject, message.TextBody);
                    }
                    else
                    {
                        await htmlMail.SendHtmlAsync(message.Recipient, message.Subject, message.TextBody, message.HtmlBody);
                    }

                    sent = clock.GetUtcNow().UtcDateTime;
                }
                catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
                {
                    // SMTP errors may contain recipient data; persist and log only the exception type.
                    error = exception.GetType().Name;
                    logger.LogWarning("E-Mail {MessageId} konnte nicht versandt werden ({Error}); erneuter Versuch folgt.", id, error);
                }

                DateTime completedAt = clock.GetUtcNow().UtcDateTime;
                DateTime next = completedAt.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(message.Attempts, 6))));
                if (db.Database.IsRelational())
                {
                    await db.MailMessages.Where(x => x.Id == id && x.LeaseId == leaseId).ExecuteUpdateAsync(update => update
                        .SetProperty(x => x.SentAtUtc, sent).SetProperty(x => x.LastError, error)
                        .SetProperty(x => x.NextAttemptAtUtc, next).SetProperty(x => x.LeaseUntilUtc, (DateTime?)null)
                        .SetProperty(x => x.LeaseId, (Guid?)null).SetProperty(x => x.ModifiedOn, completedAt), cancellationToken);
                }
                else
                {
                    message.SentAtUtc = sent;
                    message.LastError = error;
                    message.NextAttemptAtUtc = next;
                    message.LeaseUntilUtc = null;
                    message.LeaseId = null;
                    message.ModifiedOn = completedAt;
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }
}
