using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    public sealed class SaveEquipmentRequest
    {
        public Guid EquipmentTypeId { get; set; }

        [Required, StringLength(150)] public string SerialNumber { get; set; } = string.Empty;

        [Required, StringLength(150)] public string Manufacturer { get; set; } = string.Empty;

        [EnumDataType(typeof(EquipmentStatus))] public EquipmentStatus Status { get; set; } = EquipmentStatus.Ready;

        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
    }
}
