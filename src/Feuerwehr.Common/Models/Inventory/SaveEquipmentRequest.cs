using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    public sealed class SaveEquipmentRequest
    {
        public Guid EquipmentTypeId { get; set; }
        [Required, StringLength(150)] public string SerialNumber { get; set; } = "";
        [Required, StringLength(150)] public string Manufacturer { get; set; } = "";
        [EnumDataType(typeof(EquipmentStatus))] public EquipmentStatus Status { get; set; } = EquipmentStatus.Ready;
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
    }
}
