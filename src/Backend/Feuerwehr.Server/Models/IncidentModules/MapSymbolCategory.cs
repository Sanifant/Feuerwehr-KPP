using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    public sealed class MapSymbolCategory
    {
        [Key, MaxLength(40)]
        public string Name { get; set; } = string.Empty;
    }
}
