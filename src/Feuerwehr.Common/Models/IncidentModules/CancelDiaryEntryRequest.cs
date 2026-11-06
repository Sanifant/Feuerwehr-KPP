using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.IncidentModules
{
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
}
