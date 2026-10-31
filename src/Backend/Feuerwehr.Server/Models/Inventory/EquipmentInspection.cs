using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.Server.Models.Inventory
{
    public sealed class EquipmentInspection
    {
        public Guid Id { get; set; }
        public Guid EquipmentId { get; set; }
        public Equipment Equipment { get; set; } = null!;
        public Guid CommandId { get; set; }
        public DateOnly InspectedOn { get; set; }
        public DateTime RecordedAtUtc { get; set; }
        public string InspectorId { get; set; } = "";
        public string InspectorName { get; set; } = "";
        public string? Comment { get; set; }
        public EquipmentStatus Status { get; set; }
        public string EquipmentTypeName { get; set; } = "";
        public int InspectionIntervalMonths { get; set; }
        public string InspectionInstructions { get; set; } = "";
    }
}
