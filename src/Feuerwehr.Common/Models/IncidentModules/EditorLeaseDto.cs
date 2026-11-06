namespace de.openelp.feuerwehr.Common.Models.IncidentModules
{
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
