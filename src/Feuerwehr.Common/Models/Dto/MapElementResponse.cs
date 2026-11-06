using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries the result of map element operations.
    /// </summary>
    /// <param name="Element">The situation-map element returned by the operation.</param>
    /// <param name="IncidentRevision">The incident revision after the operation.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record MapElementResponse(MapElementDto Element, long IncidentRevision, DateTime ServerUtc);
}
