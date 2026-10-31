using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.Server.Models.Inventory;

public sealed class Equipment
{
    public Guid Id { get; set; }
    public int BarcodeNumber { get; set; }
    public Guid EquipmentTypeId { get; set; }
    public EquipmentType EquipmentType { get; set; } = null!;
    public string SerialNumber { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public EquipmentStatus Status { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly CreatedOn { get; set; }
    public DateOnly? LastInspectedOn { get; set; }
    public int Version { get; set; } = 1;
}
