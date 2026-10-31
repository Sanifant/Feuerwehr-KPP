using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.Server.Models.Inventory;

public sealed class EquipmentType
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public InventoryCategory Category { get; set; } = null!;
    public string Name { get; set; } = "";
    public int InspectionIntervalMonths { get; set; }
    public string InspectionInstructions { get; set; } = "";
    public int Version { get; set; } = 1;
}
