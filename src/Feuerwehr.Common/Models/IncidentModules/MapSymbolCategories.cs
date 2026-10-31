using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules;

public static class MapSymbolCategories
{
    public static readonly IReadOnlyList<string> All = Array.AsReadOnly(new[] { "Feuerwehr", "Rettungsdienst", "Personen", "Objekte" });
}
