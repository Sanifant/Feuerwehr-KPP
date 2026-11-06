namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries a live incident changed notification and the associated incident revision.
    /// </summary>
    /// <param name="IncidentId">The identifier of the incident.</param>
    /// <param name="Revision">The revision counter for the incident changed event.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record IncidentChangedEvent(Guid IncidentId, long Revision, DateTime ServerUtc);
}
