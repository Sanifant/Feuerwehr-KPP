namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries the result of diary entry created operations.
    /// </summary>
    /// <param name="EntryId">The identifier of the diary entry.</param>
    /// <param name="EntryNumber">The sequential entry number within the incident diary.</param>
    /// <param name="CurrentRevision">The latest revision number of the diary entry.</param>
    /// <param name="IncidentRevision">The incident revision after the operation.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record DiaryEntryCreatedResponse(Guid EntryId, long EntryNumber, int CurrentRevision, long IncidentRevision, DateTime ServerUtc);
}
