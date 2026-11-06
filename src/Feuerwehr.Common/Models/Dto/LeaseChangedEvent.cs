namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record LeaseChangedEvent(Guid IncidentId, string? LeaseOwner, string? LeaseSessionId, DateTime? ExpiresAtUtc, DateTime ServerUtc);
}
