using de.openelp.feuerwehr.Common.Models.IncidentModules;

namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record UnchangedMapElementResponse(MapElementDto Element, bool Unchanged, DateTime ServerUtc);
}
