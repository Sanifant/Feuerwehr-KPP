namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    public record DiaryEntryCreatedResponse(Guid EntryId, long EntryNumber, int CurrentRevision, long IncidentRevision, DateTime ServerUtc);
}
