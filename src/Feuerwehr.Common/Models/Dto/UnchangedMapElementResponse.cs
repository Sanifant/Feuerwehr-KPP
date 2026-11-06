using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    public record UnchangedMapElementResponse(MapElementDto Element, bool Unchanged, DateTime ServerUtc);
}
