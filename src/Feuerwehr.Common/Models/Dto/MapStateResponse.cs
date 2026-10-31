using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto;

public record MapStateResponse(Guid IncidentId, IncidentStatus IncidentStatus, long Revision, DateTime ServerUtc, EditorLeaseDto? Lease, IReadOnlyCollection<MapElementDto> Elements, IReadOnlyCollection<MapHydrantDto> Hydrants);
