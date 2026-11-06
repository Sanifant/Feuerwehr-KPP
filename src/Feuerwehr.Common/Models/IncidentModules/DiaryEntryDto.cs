namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Carries diary entry data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the diary entry.</param>
    /// <param name="IncidentId">The identifier of the incident.</param>
    /// <param name="EntryNumber">The sequential entry number within the incident diary.</param>
    /// <param name="CurrentRevision">The latest revision number of the diary entry.</param>
    /// <param name="IsCanceled">The diary entry has been canceled while preserving its revision history.</param>
    /// <param name="CancellationReason">The reason recorded when canceling the diary entry.</param>
    /// <param name="CreatedAtUtc">The UTC time at which the record was created.</param>
    /// <param name="UpdatedAtUtc">The UTC time of the latest change.</param>
    /// <param name="CurrentRevisionData">The content and author metadata of the latest diary revision.</param>
    /// <param name="Revisions">The revisions associated with this diary entry.</param>
    public record DiaryEntryDto(
        Guid Id,
        Guid IncidentId,
        long EntryNumber,
        int CurrentRevision,
        bool IsCanceled,
        string? CancellationReason,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc,
        DiaryRevisionDto CurrentRevisionData,
        IReadOnlyCollection<DiaryRevisionDto> Revisions);
}
