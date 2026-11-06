namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries a live map element changed notification and the associated incident revision.
    /// </summary>
    /// <param name="IncidentId">The identifier of the incident.</param>
    /// <param name="IncidentRevision">The incident revision after the operation.</param>
    /// <param name="ElementId">The identifier of the map element.</param>
    /// <param name="Action">The operation name recorded in the audit or live event.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record MapElementChangedEvent(Guid IncidentId, long IncidentRevision, Guid ElementId, string Action, DateTime ServerUtc);
}
