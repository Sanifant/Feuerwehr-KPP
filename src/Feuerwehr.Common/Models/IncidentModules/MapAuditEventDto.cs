namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Carries map audit event data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the map audit event.</param>
    /// <param name="IncidentId">The identifier of the incident.</param>
    /// <param name="SequenceNumber">The sequence number allocated within the category.</param>
    /// <param name="CommandId">The client-generated command identifier used to detect retries.</param>
    /// <param name="ElementId">The identifier of the map element.</param>
    /// <param name="Action">The operation name recorded in the audit or live event.</param>
    /// <param name="UserId">The identifier of the user account.</param>
    /// <param name="UserDisplayName">The acting user's display name.</param>
    /// <param name="OccurredAtUtc">The UTC time at which the audited change occurred.</param>
    /// <param name="BeforeStateJson">The serialized map-element state before the change.</param>
    /// <param name="AfterStateJson">The serialized map-element state after the change.</param>
    public record MapAuditEventDto(
        Guid Id,
        Guid IncidentId,
        long SequenceNumber,
        string CommandId,
        Guid? ElementId,
        string Action,
        string UserId,
        string UserDisplayName,
        DateTime OccurredAtUtc,
        string BeforeStateJson,
        string AfterStateJson);
}
