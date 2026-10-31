using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto;

public record InitialLeaseChangedEvent(Guid IncidentId, string LeaseOwner, DateTime ExpiresAtUtc, DateTime ServerUtc);
