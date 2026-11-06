using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.Inventory
{
    public sealed class SaveEquipmentTypeRequest
    {
        public Guid CategoryId { get; set; }
        [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
        [Range(1, 120)] public int InspectionIntervalMonths { get; set; } = 12;
        [Required, StringLength(8000)] public string InspectionInstructions { get; set; } = string.Empty;
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
    }
}
