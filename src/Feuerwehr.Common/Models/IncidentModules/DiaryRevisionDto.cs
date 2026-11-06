using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    public record DiaryRevisionDto(
        Guid Id,
        int RevisionNumber,
        DiaryRevisionType RevisionType,
        DateTime EventTimestampUtc,
        string CategoryCode,
        string CategoryNameSnapshot,
        string Text,
        string? Sender,
        string? Recipient,
        string? TransmissionType,
        string? CancellationReason,
        DateTime CreatedAtUtc,
        string CreatedByDisplayName);
}
