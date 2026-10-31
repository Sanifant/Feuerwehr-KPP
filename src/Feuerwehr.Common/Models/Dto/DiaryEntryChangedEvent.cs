using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    public record DiaryEntryChangedEvent(Guid IncidentId, Guid EntryId, long EntryNumber, string Action, long IncidentRevision, DateTime ServerUtc);
}
