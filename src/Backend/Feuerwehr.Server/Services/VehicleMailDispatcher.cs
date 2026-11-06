using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Executes vehicle Mail Dispatcher.
    /// </summary>
    /// <param name="db">The db.</param>
    /// <param name="mail">The mail.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="logger">The logger.</param>
    public sealed class VehicleMailDispatcher(
        FeuerwehrDbContext db,
        IHtmlMailService mail,
        TimeProvider clock,
        ILogger<VehicleMailDispatcher> logger) : IVehicleMailDispatcher
    {
        /// <summary>
        /// Executes dispatch.
        /// </summary>
        /// <param name="ct">The ct.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task Dispatch(CancellationToken ct)
        {
            DateTime now = clock.GetUtcNow().UtcDateTime;
            List<VehicleMailMessage> candidates = await db.VehicleMailMessages
                .Where(x => x.SentAtUtc == null && x.NextAttemptAtUtc <= now && (x.LeaseUntilUtc == null || x.LeaseUntilUtc < now))
                .OrderBy(x => x.CreatedAtUtc).Take(20).ToListAsync(ct);
            foreach (VehicleMailMessage? message in candidates)
            {
                ct.ThrowIfCancellationRequested();
                DateTime leaseUntil = clock.GetUtcNow().UtcDateTime.AddMinutes(5);
                if (db.Database.IsRelational())
                {
                    int claimed = await db.VehicleMailMessages.Where(x => x.Id == message.Id && x.SentAtUtc == null && x.NextAttemptAtUtc <= now &&
                        (x.LeaseUntilUtc == null || x.LeaseUntilUtc < now)).ExecuteUpdateAsync(
                            update => update
                            .SetProperty(x => x.LeaseUntilUtc, leaseUntil).SetProperty(x => x.Attempts, x => x.Attempts + 1), ct);
                    if (claimed == 0)
                    {
                        continue;
                    }
                }
                else
                {
                    message.LeaseUntilUtc = leaseUntil;
                    message.Attempts++;
                    _ = db.VehicleMailMessages.Update(message);
                    _ = await db.SaveChangesAsync(ct);
                }

                DateTime? sent = null;
                string? error = null;
                try
                {
                    await mail.SendHtmlAsync(message.Recipient, message.Subject, message.TextBody, message.HtmlBody);
                    sent = clock.GetUtcNow().UtcDateTime;
                }
                catch (Exception exception) when (!ct.IsCancellationRequested)
                {
                    error = exception.GetType().Name;
                    logger.LogWarning("MTW-E-Mail {MessageId} konnte nicht versandt werden ({Error}); erneuter Versuch folgt.", message.Id, error);
                }

                DateTime next = clock.GetUtcNow().UtcDateTime.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(message.Attempts + 1, 6))));
                if (db.Database.IsRelational())
                {
                    _ = await db.VehicleMailMessages.Where(x => x.Id == message.Id && x.LeaseUntilUtc == leaseUntil).ExecuteUpdateAsync(
                        update => update
                        .SetProperty(x => x.SentAtUtc, sent).SetProperty(x => x.LastError, error)
                        .SetProperty(x => x.NextAttemptAtUtc, next).SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), ct);
                }
                else
                {
                    message.SentAtUtc = sent;
                    message.LastError = error;
                    message.NextAttemptAtUtc = next;
                    message.LeaseUntilUtc = null;
                    _ = await db.SaveChangesAsync(ct);
                }
            }
        }
    }
}
