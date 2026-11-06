using de.openelp.feuerwehr.Common.Models.IncidentModules;

namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record IncidentCreatedResponse(IncidentSummaryDto Incident, EditorLeaseDto Lease, DateTime ServerUtc);
}
