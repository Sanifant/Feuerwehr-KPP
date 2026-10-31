using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto;

public record DiaryEntryCanceledResponse(Guid EntryId, int CurrentRevision, bool Canceled, long IncidentRevision, DateTime ServerUtc);
