namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record DiaryEntryUpdatedResponse(Guid EntryId, int CurrentRevision, long IncidentRevision, DateTime ServerUtc);
}
