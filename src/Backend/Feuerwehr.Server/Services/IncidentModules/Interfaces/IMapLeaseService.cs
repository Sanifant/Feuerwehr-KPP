using de.openelp.feuerwehr.Server.Models.IncidentModules;

namespace de.openelp.feuerwehr.Server.Services.IncidentModules
{
    public interface IMapLeaseService
    {
        Task<EditorLease> RequireValidLeaseAsync(Guid incidentId, string sessionId, string leaseToken, string userId, CancellationToken cancellationToken);
    }
}
