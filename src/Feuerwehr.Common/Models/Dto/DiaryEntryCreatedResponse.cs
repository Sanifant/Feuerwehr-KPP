namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record DiaryEntryCreatedResponse(Guid EntryId, long EntryNumber, int CurrentRevision, long IncidentRevision, DateTime ServerUtc);
}
