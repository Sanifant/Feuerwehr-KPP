using System.ComponentModel.DataAnnotations;

namespace Feuerwehr.Server.Models.IncidentModules;

public record IncidentSummaryDto(
    Guid Id,
    string Name,
    string? IncidentNumber,
    string? Keyword,
    string? Description,
    double OriginLongitude,
    double OriginLatitude,
    IncidentStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ClosedAtUtc,
    long Revision);

public class CreateIncidentRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? IncidentNumber { get; set; }

    [MaxLength(200)]
    public string? Keyword { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    public double OriginLongitude { get; set; }

    public double OriginLatitude { get; set; }

    [Required]
    [MaxLength(120)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CommandId { get; set; } = string.Empty;
}

public class CloseIncidentRequest
{
    [Required]
    [MaxLength(120)]
    public string LeaseToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CommandId { get; set; } = string.Empty;
}

public record EditorLeaseDto(
    Guid IncidentId,
    string UserId,
    string UserDisplayName,
    string SessionId,
    string LeaseToken,
    DateTime ExpiresAtUtc,
    DateTime LastHeartbeatAtUtc,
    bool IsOwnedByCurrentSession,
    bool IsExpired);

public class AcquireLeaseRequest
{
    [Required]
    [MaxLength(120)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CommandId { get; set; } = string.Empty;
}

public class LeaseHeartbeatRequest
{
    [Required]
    [MaxLength(120)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string LeaseToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CommandId { get; set; } = string.Empty;
}

public class ReleaseLeaseRequest : LeaseHeartbeatRequest;

public class UpsertMapElementRequest
{
    [Required]
    [MaxLength(200)]
    public string CommandId { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string LeaseToken { get; set; } = string.Empty;

    public Guid? ElementId { get; set; }

    [Required]
    public MapElementType ElementType { get; set; }

    [MaxLength(120)]
    public string? SymbolId { get; set; }

    [MaxLength(120)]
    public string? Category { get; set; }

    [MaxLength(200)]
    public string? Label { get; set; }

    [MaxLength(120)]
    public string? RadioCallName { get; set; }

    [MaxLength(120)]
    public string? Strength { get; set; }

    [MaxLength(2000)]
    public string? Note { get; set; }

    [MaxLength(20)]
    public string? ColorHex { get; set; }

    [Required]
    public string GeometryJson { get; set; } = string.Empty;

    public int? ExpectedVersion { get; set; }
}

public class DeleteMapElementRequest
{
    [Required]
    [MaxLength(200)]
    public string CommandId { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string LeaseToken { get; set; } = string.Empty;

    public int ExpectedVersion { get; set; }
}

public record MapElementDto(
    Guid Id,
    Guid IncidentId,
    MapElementType ElementType,
    string? SymbolId,
    string? Category,
    string? Label,
    string? RadioCallName,
    string? Strength,
    string? Note,
    string ColorHex,
    string GeometryJson,
    int Version,
    DateTime UpdatedAtUtc,
    string UpdatedByDisplayName);

public record MapAuditEventDto(
    Guid Id,
    Guid IncidentId,
    long SequenceNumber,
    string CommandId,
    Guid? ElementId,
    string Action,
    string UserId,
    string UserDisplayName,
    DateTime OccurredAtUtc,
    string BeforeStateJson,
    string AfterStateJson);

public class CreateDiaryEntryRequest
{
    [Required]
    [MaxLength(200)]
    public string CommandId { get; set; } = string.Empty;

    public DateTime EventTimestampUtc { get; set; }

    [Required]
    [MaxLength(120)]
    public string CategoryCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Text { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Sender { get; set; }

    [MaxLength(200)]
    public string? Recipient { get; set; }

    [MaxLength(120)]
    public string? TransmissionType { get; set; }
}

public class UpdateDiaryEntryRequest : CreateDiaryEntryRequest
{
    [Range(1, int.MaxValue)]
    public int ExpectedRevision { get; set; }
}

public class CancelDiaryEntryRequest
{
    [Required]
    [MaxLength(200)]
    public string CommandId { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ExpectedRevision { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

public class UpsertDiaryCategoryRequest
{
    [Required]
    [MaxLength(120)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public record DiaryCategoryDto(Guid Id, string Code, string Name, bool IsActive);

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

public class CommandEnvelopeResponse
{
    public int StatusCode { get; set; }

    public string JsonPayload { get; set; } = "{}";
}
