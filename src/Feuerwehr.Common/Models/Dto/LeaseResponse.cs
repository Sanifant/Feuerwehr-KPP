using de.openelp.feuerwehr.Common.Models.IncidentModules;

namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record LeaseResponse(EditorLeaseDto Lease, DateTime ServerUtc);
}
