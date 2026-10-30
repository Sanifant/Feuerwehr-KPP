using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.Server.Models.Inventory;

public sealed class InventoryCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
}

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

public sealed class EquipmentReminderDelivery
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public string RecipientId { get; set; } = "";
    public DateOnly DueOn { get; set; }
    public DateTime SentAtUtc { get; set; }
}

