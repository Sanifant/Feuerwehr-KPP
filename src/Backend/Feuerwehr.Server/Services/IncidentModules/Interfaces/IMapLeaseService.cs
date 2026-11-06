using De.Openelp.Feuerwehr.Server.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Defines the map lease operations required by application consumers.
    /// </summary>
    public interface IMapLeaseService
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
        Task<EditorLease> RequireValidLeaseAsync(Guid incidentId, string sessionId, string leaseToken, string userId, CancellationToken cancellationToken);
    }
}
