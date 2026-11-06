using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.IncidentModules
{
    public class UpdateDiaryEntryRequest : CreateDiaryEntryRequest
    {
        [Range(1, int.MaxValue)]
        public int ExpectedRevision { get; set; }
    }
}
