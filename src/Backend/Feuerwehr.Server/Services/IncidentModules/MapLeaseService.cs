using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Implements map lease operations for the API application layer.
    /// </summary>
    /// <param name="dbContext">The database context for the current unit of work.</param>
    public sealed class MapLeaseService(FeuerwehrDbContext dbContext) : IMapLeaseService
    {
        /// <summary>
        /// Requires an unexpired map editing lease matching the incident, user, session, and token.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="sessionId">The client session that owns or requests the editing lease.</param>
        /// <param name="leaseToken">The opaque token proving ownership of the editing lease.</param>
        /// <param name="userId">The identifier of the user account.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the validated editing lease owned by the supplied user and session.</returns>
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
