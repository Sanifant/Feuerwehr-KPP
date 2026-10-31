using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    public record LeaseChangedEvent(Guid IncidentId, string? LeaseOwner, string? LeaseSessionId, DateTime? ExpiresAtUtc, DateTime ServerUtc);
}
