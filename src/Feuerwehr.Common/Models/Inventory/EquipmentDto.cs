using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
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
}
