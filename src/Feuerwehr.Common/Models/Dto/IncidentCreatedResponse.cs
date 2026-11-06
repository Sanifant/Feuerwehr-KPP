using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries the result of incident created operations.
    /// </summary>
    /// <param name="Incident">The incident associated with this incident created.</param>
    /// <param name="Lease">The current situation-map editing lease.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record IncidentCreatedResponse(IncidentSummaryDto Incident, EditorLeaseDto Lease, DateTime ServerUtc);
}
