using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries the result of map state operations.
    /// </summary>
    /// <param name="IncidentId">The identifier of the incident.</param>
    /// <param name="IncidentStatus">The incident status associated with this map state.</param>
    /// <param name="Revision">The revision counter for the map state.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    /// <param name="Lease">The current situation-map editing lease.</param>
    /// <param name="Elements">The elements associated with this map state.</param>
    /// <param name="Hydrants">The hydrants associated with this map state.</param>
    public record MapStateResponse(Guid IncidentId, IncidentStatus IncidentStatus, long Revision, DateTime ServerUtc, EditorLeaseDto? Lease, IReadOnlyCollection<MapElementDto> Elements, IReadOnlyCollection<MapHydrantDto> Hydrants);
}
