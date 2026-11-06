using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    public record MapElementDeletedResponse(bool Deleted, Guid ElementId, long IncidentRevision, DateTime ServerUtc);
}
