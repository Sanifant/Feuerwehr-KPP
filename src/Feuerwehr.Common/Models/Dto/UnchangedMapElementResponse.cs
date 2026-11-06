using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries the result of unchanged map element operations.
    /// </summary>
    /// <param name="Element">The situation-map element returned by the operation.</param>
    /// <param name="Unchanged">The request left the existing data unchanged.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record UnchangedMapElementResponse(MapElementDto Element, bool Unchanged, DateTime ServerUtc);
}
