namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Carries equipment type data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the equipment type.</param>
    /// <param name="CategoryId">The identifier of the category assigned to the equipment type.</param>
    /// <param name="CategoryName">The category's display name.</param>
    /// <param name="Name">The display name of the equipment type.</param>
    /// <param name="InspectionIntervalMonths">The inspection interval in calendar months.</param>
    /// <param name="InspectionInstructions">The instructions for performing the equipment inspection.</param>
    /// <param name="Version">The concurrency version used to detect conflicting updates.</param>
    public sealed record EquipmentTypeDto(
        Guid Id,
        Guid CategoryId,
        string CategoryName,
        string Name,
        int InspectionIntervalMonths,
        string InspectionInstructions,
        int Version);
}
