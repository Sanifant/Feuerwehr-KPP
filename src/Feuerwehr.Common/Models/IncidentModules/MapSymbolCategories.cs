namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Defines the standard category identifiers used by situation-map symbols.
    /// </summary>
    public static class MapSymbolCategories
    {
        /// <summary>
        /// The the all associated with this map symbol categories.
        /// </summary>
        public static readonly IReadOnlyList<string> All = Array.AsReadOnly(new[] { "Feuerwehr", "Rettungsdienst", "Personen", "Objekte" });
    }
}
