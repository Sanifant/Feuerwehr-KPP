using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules;

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
