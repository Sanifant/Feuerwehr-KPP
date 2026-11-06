namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries the result of map element deleted operations.
    /// </summary>
    /// <param name="Deleted">The requested record was deleted.</param>
    /// <param name="ElementId">The identifier of the map element.</param>
    /// <param name="IncidentRevision">The incident revision after the operation.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record MapElementDeletedResponse(bool Deleted, Guid ElementId, long IncidentRevision, DateTime ServerUtc);
}
