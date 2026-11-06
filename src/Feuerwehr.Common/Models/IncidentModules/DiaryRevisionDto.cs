namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Carries diary revision data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the diary revision.</param>
    /// <param name="RevisionNumber">The sequential number of this diary revision.</param>
    /// <param name="RevisionType">The kind of diary revision: creation, update, or cancellation.</param>
    /// <param name="EventTimestampUtc">The UTC time of the event described by the diary entry.</param>
    /// <param name="CategoryCode">The category code used in equipment barcode allocation.</param>
    /// <param name="CategoryNameSnapshot">The category name captured when the inspection was recorded.</param>
    /// <param name="Text">The text content of the diary revision.</param>
    /// <param name="Sender">The configured sender email address.</param>
    /// <param name="Recipient">The destination email address.</param>
    /// <param name="TransmissionType">The transmission channel recorded in the diary entry.</param>
    /// <param name="CancellationReason">The reason recorded when canceling the diary entry.</param>
    /// <param name="CreatedAtUtc">The UTC time at which the record was created.</param>
    /// <param name="CreatedByDisplayName">The creator's display name captured with the record.</param>
    public record DiaryRevisionDto(
        Guid Id,
        int RevisionNumber,
        DiaryRevisionType RevisionType,
        DateTime EventTimestampUtc,
        string CategoryCode,
        string CategoryNameSnapshot,
        string Text,
        string? Sender,
        string? Recipient,
        string? TransmissionType,
        string? CancellationReason,
        DateTime CreatedAtUtc,
        string CreatedByDisplayName);
}
