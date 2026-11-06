using de.openelp.feuerwehr.Common.Models.IncidentModules;

namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record IncidentClosedResponse(IncidentSummaryDto Incident, DateTime ServerUtc);
}
