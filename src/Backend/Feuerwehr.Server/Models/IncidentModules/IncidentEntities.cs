using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Feuerwehr.Server.Models.IncidentModules;

public enum IncidentStatus
{
    Active = 1,
    Closed = 2,
}

public enum MapElementType
{
    Symbol = 1,
    Polygon = 2,
}

public enum DiaryRevisionType
{
    Created = 1,
    Edited = 2,
    Canceled = 3,
}

public class Incident
{
    [Key]
    public Guid Id { get; set; }

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

    public IncidentStatus Status { get; set; } = IncidentStatus.Active;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ClosedAtUtc { get; set; }

    [MaxLength(450)]
    public string? ClosedByUserId { get; set; }

    [MaxLength(200)]
    public string? ClosedByDisplayName { get; set; }

    public long Revision { get; set; }
}

public class MapElement
{
    [Key]
    public Guid Id { get; set; }

    public Guid IncidentId { get; set; }

    [ForeignKey(nameof(IncidentId))]
    public Incident Incident { get; set; } = null!;

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
    public string ColorHex { get; set; } = "#d62828";

    [Required]
    public string GeometryJson { get; set; } = string.Empty;

    public int Version { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    [MaxLength(450)]
    public string UpdatedByUserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string UpdatedByDisplayName { get; set; } = string.Empty;
}

public class MapAuditEvent
{
    [Key]
    public Guid Id { get; set; }

    public Guid IncidentId { get; set; }

    public long SequenceNumber { get; set; }

    [MaxLength(200)]
    public string CommandId { get; set; } = string.Empty;

    public Guid? ElementId { get; set; }

    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string UserDisplayName { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    [Required]
    public string BeforeStateJson { get; set; } = "null";

    [Required]
    public string AfterStateJson { get; set; } = "null";
}

public class EditorLease
{
    [Key]
    public Guid Id { get; set; }

    public Guid IncidentId { get; set; }

    [ForeignKey(nameof(IncidentId))]
    public Incident Incident { get; set; } = null!;

    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string UserDisplayName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string SessionId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string LeaseToken { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime LastHeartbeatAtUtc { get; set; }
}

public class DiaryCategory
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(120)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public class DiaryEntry
{
    [Key]
    public Guid Id { get; set; }

    public Guid IncidentId { get; set; }

    [ForeignKey(nameof(IncidentId))]
    public Incident Incident { get; set; } = null!;

    public long EntryNumber { get; set; }

    public int CurrentRevision { get; set; }

    public bool IsCanceled { get; set; }

    [MaxLength(1000)]
    public string? CancellationReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public class DiaryEntryRevision
{
    [Key]
    public Guid Id { get; set; }

    public Guid DiaryEntryId { get; set; }

    [ForeignKey(nameof(DiaryEntryId))]
    public DiaryEntry DiaryEntry { get; set; } = null!;

    public int RevisionNumber { get; set; }

    public DiaryRevisionType RevisionType { get; set; }

    public DateTime EventTimestampUtc { get; set; }

    [MaxLength(120)]
    public string CategoryCode { get; set; } = string.Empty;

    [MaxLength(120)]
    public string CategoryNameSnapshot { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Text { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Sender { get; set; }

    [MaxLength(200)]
    public string? Recipient { get; set; }

    [MaxLength(120)]
    public string? TransmissionType { get; set; }

    [MaxLength(1000)]
    public string? CancellationReason { get; set; }

    [MaxLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CreatedByDisplayName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}

public class ProcessedCommand
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string CommandId { get; set; } = string.Empty;

    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Scope { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TargetId { get; set; }

    [MaxLength(200)]
    public string PayloadHash { get; set; } = string.Empty;

    public int ResponseStatusCode { get; set; }

    public string ResponseJson { get; set; } = "{}";

    public DateTime CreatedAtUtc { get; set; }
}

public class OutboxMessage
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(120)]
    public string AggregateType { get; set; } = string.Empty;

    [MaxLength(120)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(200)]
    public string AggregateId { get; set; } = string.Empty;

    [Required]
    public string PayloadJson { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public int DeliveryAttempts { get; set; }

    [MaxLength(2000)]
    public string? LastError { get; set; }
}
