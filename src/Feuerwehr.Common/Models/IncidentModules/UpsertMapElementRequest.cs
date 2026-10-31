using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
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
}
