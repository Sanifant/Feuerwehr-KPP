using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules;

public class UpdateDiaryEntryRequest : CreateDiaryEntryRequest
{
    [Range(1, int.MaxValue)]
    public int ExpectedRevision { get; set; }
}
