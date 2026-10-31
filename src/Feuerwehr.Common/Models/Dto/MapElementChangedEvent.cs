using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto;

public record MapElementChangedEvent(Guid IncidentId, long IncidentRevision, Guid ElementId, string Action, DateTime ServerUtc);
