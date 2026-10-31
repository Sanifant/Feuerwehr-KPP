using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory;

public sealed class EquipmentTypeImportDto
{
    public Guid? Id { get; set; }
    public Guid? CategoryId { get; set; }
    [StringLength(100, ErrorMessage = "Der Kategoriename darf hÃ¶chstens 100 Zeichen enthalten.")] public string? CategoryName { get; set; }
    [Required(ErrorMessage = "Die Typbezeichnung ist erforderlich."), StringLength(150, ErrorMessage = "Die Typbezeichnung darf hÃ¶chstens 150 Zeichen enthalten.")]
    public string Name { get; set; } = "";
    [Range(1, 120, ErrorMessage = "Das PrÃ¼fintervall muss zwischen 1 und 120 Monaten liegen.")] public int InspectionIntervalMonths { get; set; }
    [Required(ErrorMessage = "Eine PrÃ¼fanweisung ist erforderlich."), StringLength(8000, ErrorMessage = "Die PrÃ¼fanweisung darf hÃ¶chstens 8000 Zeichen enthalten.")]
    public string InspectionInstructions { get; set; } = "";
}
