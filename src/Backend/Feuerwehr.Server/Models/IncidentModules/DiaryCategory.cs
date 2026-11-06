using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
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
}
