using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory;

/// <summary>
/// Executes inventory Import Error.
/// </summary>
/// <param name="LineNumber">The line Number.</param>
/// <param name="Description">The description.</param>
public sealed record InventoryImportError(long? LineNumber, string Description);
/// <summary>
/// Executes inventory Import Response.
/// </summary>
/// <param name="CreatedCount">The created Count.</param>
/// <param name="UpdatedCount">The updated Count.</param>
/// <param name="Errors">The errors.</param>
public sealed record InventoryImportResponse(int CreatedCount, int UpdatedCount, IReadOnlyList<InventoryImportError> Errors);

public sealed class InventoryCategoryImportDto
{
    public Guid? Id { get; set; }
    [Required(ErrorMessage = "Der Kategoriename ist erforderlich."), StringLength(100, ErrorMessage = "Der Kategoriename darf hÃ¶chstens 100 Zeichen enthalten.")]
    public string Name { get; set; } = "";
}

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

