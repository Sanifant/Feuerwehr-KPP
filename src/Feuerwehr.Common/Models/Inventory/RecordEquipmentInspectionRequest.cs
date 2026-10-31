using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    public sealed class RecordEquipmentInspectionRequest
    {
        public Guid CommandId { get; set; }
        public DateOnly InspectedOn { get; set; }
        [EnumDataType(typeof(EquipmentStatus))] public EquipmentStatus Status { get; set; }
        [StringLength(4000)] public string? Comment { get; set; }
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
    }
}
