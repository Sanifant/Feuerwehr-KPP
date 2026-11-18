namespace de.openelp.feuerwehr.Common.Models.HydrantInspections
{
    /// <summary>Shared representation of a hydrant inspection without persistence navigation properties.</summary>
    /// <param name="Id">The inspection identifier.</param>
    /// <param name="HydrantId">The inspected hydrant identifier.</param>
    /// <param name="InspectedOn">The inspection date.</param>
    /// <param name="InspectedAtUtc">The inspection timestamp in UTC.</param>
    /// <param name="InspectorId">The inspecting user's identifier.</param>
    /// <param name="InspectorName">The inspecting user's readable name.</param>
    /// <param name="Comment">The optional inspection comment.</param>
    /// <param name="PreviousStatus">The hydrant status before the inspection.</param>
    /// <param name="NewStatus">The hydrant status after the inspection.</param>
    /// <param name="PhotoStorageKey">The optional relative photo storage key.</param>
    /// <param name="PhotoContentType">The optional photo content type.</param>
    public sealed record HydrantInspectionDto(
        Guid Id,
        int HydrantId,
        DateOnly InspectedOn,
        DateTime InspectedAtUtc,
        string InspectorId,
        string InspectorName,
        string? Comment,
        HydrantStatus PreviousStatus,
        HydrantStatus NewStatus,
        string? PhotoStorageKey,
        string? PhotoContentType);
}
