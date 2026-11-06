using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.Inventory
{
    public sealed class CreateInventoryCategoryRequest
    {
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    }
}
