namespace de.openelp.feuerwehr.Server.Models.Inventory
{
    public sealed class EquipmentType
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public InventoryCategory Category { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public int InspectionIntervalMonths { get; set; }
        public string InspectionInstructions { get; set; } = string.Empty;
        public int Version { get; set; } = 1;
    }
}
