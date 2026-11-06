namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Carries equipment data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the equipment.</param>
    /// <param name="Barcode">The unique equipment barcode used for lookup and import matching.</param>
    /// <param name="EquipmentTypeId">The identifier of the equipment type defining its inspection plan.</param>
    /// <param name="EquipmentTypeName">The display name of the equipment type.</param>
    /// <param name="CategoryName">The category's display name.</param>
    /// <param name="SerialNumber">The manufacturer's serial number of the equipment.</param>
    /// <param name="Manufacturer">The equipment manufacturer.</param>
    /// <param name="Status">The operational condition of the equipment.</param>
    /// <param name="IsActive">The equipment is active.</param>
    /// <param name="CreatedOn">The date the equipment was registered.</param>
    /// <param name="LastInspectedOn">The most recent inspection date, or null when no inspection has been recorded.</param>
    /// <param name="NextInspectionOn">The next inspection due date calculated from the inspection plan.</param>
    /// <param name="InspectionIntervalMonths">The inspection interval in calendar months.</param>
    /// <param name="InspectionInstructions">The instructions for performing the equipment inspection.</param>
    /// <param name="Version">The concurrency version used to detect conflicting updates.</param>
    public sealed record EquipmentDto(
        Guid Id,
        string Barcode,
        Guid EquipmentTypeId,
        string EquipmentTypeName,
        string CategoryName,
        string SerialNumber,
        string Manufacturer,
        EquipmentStatus Status,
        bool IsActive,
        DateOnly CreatedOn,
        DateOnly? LastInspectedOn,
        DateOnly NextInspectionOn,
        int InspectionIntervalMonths,
        string InspectionInstructions,
        int Version);
}
