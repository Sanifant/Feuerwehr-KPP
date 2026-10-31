using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto;

public record IncidentCreatedResponse(IncidentSummaryDto Incident, EditorLeaseDto Lease, DateTime ServerUtc);
