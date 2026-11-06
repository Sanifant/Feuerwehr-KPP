namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Carries equipment inspection data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the equipment inspection.</param>
    /// <param name="InspectedOn">The date on which the equipment inspection took place.</param>
    /// <param name="RecordedAtUtc">The UTC time at which this record was saved.</param>
    /// <param name="InspectorName">The inspector's display name captured with the inspection.</param>
    /// <param name="Comment">The comment recorded with the equipment inspection.</param>
    /// <param name="Status">The operational condition of the equipment.</param>
    /// <param name="EquipmentTypeName">The display name of the equipment type.</param>
    /// <param name="InspectionIntervalMonths">The inspection interval in calendar months.</param>
    /// <param name="InspectionInstructions">The instructions for performing the equipment inspection.</param>
    public sealed record EquipmentInspectionDto(
        Guid Id,
        DateOnly InspectedOn,
        DateTime RecordedAtUtc,
        string InspectorName,
        string? Comment,
        EquipmentStatus Status,
        string EquipmentTypeName,
        int InspectionIntervalMonths,
        string InspectionInstructions);
}
