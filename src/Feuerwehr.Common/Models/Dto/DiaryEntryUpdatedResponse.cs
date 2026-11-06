namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries the result of diary entry updated operations.
    /// </summary>
    /// <param name="EntryId">The identifier of the diary entry.</param>
    /// <param name="CurrentRevision">The latest revision number of the diary entry.</param>
    /// <param name="IncidentRevision">The incident revision after the operation.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record DiaryEntryUpdatedResponse(Guid EntryId, int CurrentRevision, long IncidentRevision, DateTime ServerUtc);
}
