using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Executes map Lease Service.
    /// </summary>
    /// <param name="dbContext">The db Context.</param>
    public sealed class MapLeaseService(FeuerwehrDbContext dbContext) : IMapLeaseService
    {
        /// <summary>
        /// Executes require Valid Lease.
        /// </summary>
        /// <param name="incidentId">The incident Id.</param>
        /// <param name="sessionId">The session Id.</param>
        /// <param name="leaseToken">The lease Token.</param>
        /// <param name="userId">The user Id.</param>
        /// <param name="cancellationToken">The cancellation Token.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task<EditorLease> RequireValidLeaseAsync(Guid incidentId, string sessionId, string leaseToken, string userId, CancellationToken cancellationToken)
        {
            EditorLease lease = await dbContext.EditorLeases.FirstOrDefaultAsync(x => x.IncidentId == incidentId, cancellationToken)
                        ?? throw new InvalidOperationException("Keine aktive Bearbeitungssperre vorhanden.");

            DateTime now = DateTime.UtcNow;
            if (lease.ExpiresAtUtc <= now)
            {
                throw new InvalidOperationException("Die Bearbeitungssperre ist abgelaufen.");
            }

            if (!string.Equals(lease.UserId, userId, StringComparison.Ordinal)
                || !string.Equals(lease.SessionId, sessionId, StringComparison.Ordinal)
                || !string.Equals(lease.LeaseToken, leaseToken, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Diese Sitzung besitzt keine gültige Bearbeitungssperre.");
            }

            return lease;
        }
    }
}
