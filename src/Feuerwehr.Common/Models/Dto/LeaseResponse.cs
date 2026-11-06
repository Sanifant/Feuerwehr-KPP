using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries the result of lease operations.
    /// </summary>
    /// <param name="Lease">The current situation-map editing lease.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record LeaseResponse(EditorLeaseDto Lease, DateTime ServerUtc);
}
