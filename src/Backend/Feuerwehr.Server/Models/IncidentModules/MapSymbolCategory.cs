using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Server.Models.IncidentModules
{
    /// <summary>
    /// Defines a category used to organize situation-map symbols.
    /// </summary>
    public sealed class MapSymbolCategory
    {
        /// <summary>
        /// Gets or sets the display name of the map symbol category.
        /// </summary>
        [Key, MaxLength(40)]
        public string Name { get; set; } = string.Empty;
    }
}
