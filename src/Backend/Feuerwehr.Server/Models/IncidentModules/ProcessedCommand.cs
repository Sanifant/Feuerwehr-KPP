using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
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
}
