using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Specifies the input for update diary entry operations.
    /// </summary>
    public class UpdateDiaryEntryRequest : CreateDiaryEntryRequest
    {
        /// <summary>
        /// Gets or sets the diary revision the caller expects to update.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int ExpectedRevision { get; set; }
    }
}
