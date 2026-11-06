namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Carries editor lease data across the API boundary.
    /// </summary>
    /// <param name="IncidentId">The identifier of the incident.</param>
    /// <param name="UserId">The identifier of the user account.</param>
    /// <param name="UserDisplayName">The acting user's display name.</param>
    /// <param name="SessionId">The client session that owns or requests the editing lease.</param>
    /// <param name="LeaseToken">The opaque token proving ownership of the editing lease.</param>
    /// <param name="ExpiresAtUtc">The UTC expiry time of the invitation or lease.</param>
    /// <param name="LastHeartbeatAtUtc">The UTC time of the most recent editor heartbeat.</param>
    /// <param name="IsOwnedByCurrentSession">The requesting session owns the editing lease.</param>
    /// <param name="IsExpired">The editing lease has expired.</param>
    public record EditorLeaseDto(
        Guid IncidentId,
        string UserId,
        string UserDisplayName,
        string SessionId,
        string LeaseToken,
        DateTime ExpiresAtUtc,
        DateTime LastHeartbeatAtUtc,
        bool IsOwnedByCurrentSession,
        bool IsExpired);
}
