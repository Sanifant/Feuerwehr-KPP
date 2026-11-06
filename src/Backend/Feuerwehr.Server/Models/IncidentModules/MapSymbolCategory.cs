using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Server.Models.IncidentModules
{
    public sealed class MapSymbolCategory
    {
        [Key, MaxLength(40)]
        public string Name { get; set; } = string.Empty;
    }
}
