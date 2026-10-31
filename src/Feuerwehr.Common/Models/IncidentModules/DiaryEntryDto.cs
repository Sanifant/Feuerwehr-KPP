using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules;

public record DiaryEntryDto(
    Guid Id,
    Guid IncidentId,
    long EntryNumber,
    int CurrentRevision,
    bool IsCanceled,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DiaryRevisionDto CurrentRevisionData,
    IReadOnlyCollection<DiaryRevisionDto> Revisions);
