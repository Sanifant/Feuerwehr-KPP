using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace de.openelp.feuerwehr.Common.Models.Inventory;

[JsonConverter(typeof(JsonStringEnumConverter<EquipmentStatus>))]
public enum EquipmentStatus { Ready, Defective, InRepair }

public sealed record InventoryCategoryDto(Guid Id, string Name);
public sealed record EquipmentTypeDto(Guid Id, Guid CategoryId, string CategoryName, string Name,
    int InspectionIntervalMonths, string InspectionInstructions, int Version);
public sealed record EquipmentDto(Guid Id, string Barcode, Guid EquipmentTypeId, string EquipmentTypeName,
    string CategoryName, string SerialNumber, string Manufacturer, EquipmentStatus Status, bool IsActive,
    DateOnly CreatedOn, DateOnly? LastInspectedOn, DateOnly NextInspectionOn,
    int InspectionIntervalMonths, string InspectionInstructions, int Version);
public sealed record EquipmentInspectionDto(Guid Id, DateOnly InspectedOn, DateTime RecordedAtUtc,
    string InspectorName, string? Comment, EquipmentStatus Status, string EquipmentTypeName,
    int InspectionIntervalMonths, string InspectionInstructions);
public sealed record EquipmentDetailDto(EquipmentDto Equipment, IReadOnlyList<EquipmentInspectionDto> Inspections);

public sealed class CreateInventoryCategoryRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
}

public sealed class SaveEquipmentTypeRequest
{
    public Guid CategoryId { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [Range(1, 120)] public int InspectionIntervalMonths { get; set; } = 12;
    [Required, StringLength(8000)] public string InspectionInstructions { get; set; } = "";
    [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
}

public sealed class SaveEquipmentRequest
{
    public Guid EquipmentTypeId { get; set; }
    [Required, StringLength(150)] public string SerialNumber { get; set; } = "";
    [Required, StringLength(150)] public string Manufacturer { get; set; } = "";
    [EnumDataType(typeof(EquipmentStatus))] public EquipmentStatus Status { get; set; } = EquipmentStatus.Ready;
    [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
}

public sealed class RecordEquipmentInspectionRequest
{
    public Guid CommandId { get; set; }
    public DateOnly InspectedOn { get; set; }
    [EnumDataType(typeof(EquipmentStatus))] public EquipmentStatus Status { get; set; }
    [StringLength(4000)] public string? Comment { get; set; }
    [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
}

public sealed class SetEquipmentActiveRequest
{
    public bool IsActive { get; set; }
    [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
}
