namespace de.openelp.feuerwehr.Common.Models.HydrantInspections
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>Records an inspection using only the editable inspection fields.</summary>
    public sealed class CreateHydrantInspectionRequest
    {
        [MaxLength(500)]
        public string? Comment { get; set; }

        [EnumDataType(typeof(HydrantStatus))]
        public HydrantStatus NewStatus { get; set; }
    }
}
