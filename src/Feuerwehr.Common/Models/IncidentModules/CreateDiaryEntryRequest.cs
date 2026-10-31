using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules;

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
