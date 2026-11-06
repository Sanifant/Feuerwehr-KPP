namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries the result of diary entry canceled operations.
    /// </summary>
    /// <param name="EntryId">The identifier of the diary entry.</param>
    /// <param name="CurrentRevision">The latest revision number of the diary entry.</param>
    /// <param name="Canceled">The requested entry was canceled.</param>
    /// <param name="IncidentRevision">The incident revision after the operation.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record DiaryEntryCanceledResponse(Guid EntryId, int CurrentRevision, bool Canceled, long IncidentRevision, DateTime ServerUtc);
}
