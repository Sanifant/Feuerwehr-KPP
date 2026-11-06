namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries a live lease changed notification and the associated incident revision.
    /// </summary>
    /// <param name="IncidentId">The identifier of the incident.</param>
    /// <param name="LeaseOwner">The display name of the editor holding the lease.</param>
    /// <param name="LeaseSessionId">The session identifier recorded for the editing lease.</param>
    /// <param name="ExpiresAtUtc">The UTC expiry time of the invitation or lease.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record LeaseChangedEvent(Guid IncidentId, string? LeaseOwner, string? LeaseSessionId, DateTime? ExpiresAtUtc, DateTime ServerUtc);
}
