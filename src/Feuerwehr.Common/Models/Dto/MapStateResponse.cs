using de.openelp.feuerwehr.Common.Models.IncidentModules;

namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record MapStateResponse(Guid IncidentId, IncidentStatus IncidentStatus, long Revision, DateTime ServerUtc, EditorLeaseDto? Lease, IReadOnlyCollection<MapElementDto> Elements, IReadOnlyCollection<MapHydrantDto> Hydrants);
}
