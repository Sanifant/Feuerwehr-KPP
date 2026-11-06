using de.openelp.feuerwehr.Common.Models.IncidentModules;

namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record MapElementResponse(MapElementDto Element, long IncidentRevision, DateTime ServerUtc);
}
