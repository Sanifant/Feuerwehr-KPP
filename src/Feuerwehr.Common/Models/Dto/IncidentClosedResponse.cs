using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    public record IncidentClosedResponse(IncidentSummaryDto Incident, DateTime ServerUtc);
}
