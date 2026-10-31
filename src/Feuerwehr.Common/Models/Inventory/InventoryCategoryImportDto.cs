using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory;

public sealed class InventoryCategoryImportDto
{
    public Guid? Id { get; set; }
    [Required(ErrorMessage = "Der Kategoriename ist erforderlich."), StringLength(100, ErrorMessage = "Der Kategoriename darf hÃ¶chstens 100 Zeichen enthalten.")]
    public string Name { get; set; } = "";
}
