using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory;

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
