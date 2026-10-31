using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    public record MapElementResponse(MapElementDto Element, long IncidentRevision, DateTime ServerUtc);
}
