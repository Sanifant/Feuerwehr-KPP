using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory;

[JsonConverter(typeof(JsonStringEnumConverter<EquipmentStatus>))]
public enum EquipmentStatus { Ready, Defective, InRepair }

/// <summary>
/// Executes inventory Category Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
public sealed record InventoryCategoryDto(Guid Id, string Name);
/// <summary>
/// Executes equipment Type Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="CategoryId">The category Id.</param>
/// <param name="CategoryName">The category Name.</param>
/// <param name="Name">The name.</param>
/// <param name="InspectionIntervalMonths">The inspection Interval Months.</param>
/// <param name="InspectionInstructions">The inspection Instructions.</param>
/// <param name="Version">The version.</param>
public sealed record EquipmentTypeDto(Guid Id, Guid CategoryId, string CategoryName, string Name,
    int InspectionIntervalMonths, string InspectionInstructions, int Version);
/// <summary>
/// Executes equipment Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Barcode">The barcode.</param>
/// <param name="EquipmentTypeId">The equipment Type Id.</param>
/// <param name="EquipmentTypeName">The equipment Type Name.</param>
/// <param name="CategoryName">The category Name.</param>
/// <param name="SerialNumber">The serial Number.</param>
/// <param name="Manufacturer">The manufacturer.</param>
/// <param name="Status">The status.</param>
/// <param name="IsActive">The is Active.</param>
/// <param name="CreatedOn">The created On.</param>
/// <param name="LastInspectedOn">The last Inspected On.</param>
/// <param name="NextInspectionOn">The next Inspection On.</param>
/// <param name="InspectionIntervalMonths">The inspection Interval Months.</param>
/// <param name="InspectionInstructions">The inspection Instructions.</param>
/// <param name="Version">The version.</param>
public sealed record EquipmentDto(Guid Id, string Barcode, Guid EquipmentTypeId, string EquipmentTypeName,
    string CategoryName, string SerialNumber, string Manufacturer, EquipmentStatus Status, bool IsActive,
    DateOnly CreatedOn, DateOnly? LastInspectedOn, DateOnly NextInspectionOn,
    int InspectionIntervalMonths, string InspectionInstructions, int Version);
/// <summary>
/// Executes equipment Inspection Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="InspectedOn">The inspected On.</param>
/// <param name="RecordedAtUtc">The recorded At Utc.</param>
/// <param name="InspectorName">The inspector Name.</param>
/// <param name="Comment">The comment.</param>
/// <param name="Status">The status.</param>
/// <param name="EquipmentTypeName">The equipment Type Name.</param>
/// <param name="InspectionIntervalMonths">The inspection Interval Months.</param>
/// <param name="InspectionInstructions">The inspection Instructions.</param>
public sealed record EquipmentInspectionDto(Guid Id, DateOnly InspectedOn, DateTime RecordedAtUtc,
    string InspectorName, string? Comment, EquipmentStatus Status, string EquipmentTypeName,
    int InspectionIntervalMonths, string InspectionInstructions);
/// <summary>
/// Executes equipment Detail Dto.
/// </summary>
/// <param name="Equipment">The equipment.</param>
/// <param name="Inspections">The inspections.</param>
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

