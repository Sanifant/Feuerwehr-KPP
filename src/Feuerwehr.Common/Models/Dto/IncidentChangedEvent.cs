using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    public record IncidentChangedEvent(Guid IncidentId, long Revision, DateTime ServerUtc);
}
