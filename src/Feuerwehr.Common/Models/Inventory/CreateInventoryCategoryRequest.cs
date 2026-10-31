using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    public sealed class CreateInventoryCategoryRequest
    {
        [Required, StringLength(100)] public string Name { get; set; } = "";
    }
}
