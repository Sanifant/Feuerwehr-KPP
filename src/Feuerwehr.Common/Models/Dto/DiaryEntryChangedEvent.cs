namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries a live diary entry changed notification and the associated incident revision.
    /// </summary>
    /// <param name="IncidentId">The identifier of the incident.</param>
    /// <param name="EntryId">The identifier of the diary entry.</param>
    /// <param name="EntryNumber">The sequential entry number within the incident diary.</param>
    /// <param name="Action">The operation name recorded in the audit or live event.</param>
    /// <param name="IncidentRevision">The incident revision after the operation.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record DiaryEntryChangedEvent(Guid IncidentId, Guid EntryId, long EntryNumber, string Action, long IncidentRevision, DateTime ServerUtc);
}
