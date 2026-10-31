using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory;

public sealed class SaveEquipmentTypeRequest
{
    public Guid CategoryId { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [Range(1, 120)] public int InspectionIntervalMonths { get; set; } = 12;
    [Required, StringLength(8000)] public string InspectionInstructions { get; set; } = "";
    [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
}
