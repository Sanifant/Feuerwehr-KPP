using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules;

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
