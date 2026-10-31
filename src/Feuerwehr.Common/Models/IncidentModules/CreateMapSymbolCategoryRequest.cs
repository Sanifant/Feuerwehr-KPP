using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules;

public sealed class CreateMapSymbolCategoryRequest
{
    [Required, MaxLength(40)]
    public string Name { get; set; } = string.Empty;
}
