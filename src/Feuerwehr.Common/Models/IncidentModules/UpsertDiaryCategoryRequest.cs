using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
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
}
