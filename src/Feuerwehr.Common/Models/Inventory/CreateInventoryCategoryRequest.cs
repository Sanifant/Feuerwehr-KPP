using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    public sealed class CreateInventoryCategoryRequest
    {
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    }
}
