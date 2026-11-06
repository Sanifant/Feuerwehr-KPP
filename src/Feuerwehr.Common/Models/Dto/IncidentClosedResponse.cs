using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries the result of incident closed operations.
    /// </summary>
    /// <param name="Incident">The incident associated with this incident closed.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record IncidentClosedResponse(IncidentSummaryDto Incident, DateTime ServerUtc);
}
