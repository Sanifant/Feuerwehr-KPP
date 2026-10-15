using de.openelp.feuerwehr.Common.Models.IncidentModules;

namespace de.openelp.feuerwehr.Common.Models.Dto;

public record MessageResponse(string Message);

public record IdentityErrorDto(string Code, string Description);

public record IdentityErrorResponse(string Message, IReadOnlyCollection<IdentityErrorDto> Errors);

public record UserDetailsDto(string Id, string? Email, string FirstName, string LastName, string FullName, int? FireDepartmentId, bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt, IReadOnlyCollection<string> Roles);

public record UserSummaryDto(string Id, string? Email, string FirstName, string LastName, string FullName, int? FireDepartmentId, bool IsActive, IReadOnlyCollection<string> Roles);

public record IncidentCreatedResponse(IncidentSummaryDto Incident, EditorLeaseDto Lease, DateTime ServerUtc);

public record IncidentClosedResponse(IncidentSummaryDto Incident, DateTime ServerUtc);

public record LeaseResponse(EditorLeaseDto Lease, DateTime ServerUtc);

public record LeaseReleasedResponse(bool Released, DateTime ServerUtc);

public record UnchangedMapElementResponse(MapElementDto Element, bool Unchanged, DateTime ServerUtc);

public record MapElementResponse(MapElementDto Element, long IncidentRevision, DateTime ServerUtc);

public record MapElementDeletedResponse(bool Deleted, Guid ElementId, long IncidentRevision, DateTime ServerUtc);

public record DiaryEntryCreatedResponse(Guid EntryId, long EntryNumber, int CurrentRevision, long IncidentRevision, DateTime ServerUtc);

public record DiaryEntryUpdatedResponse(Guid EntryId, int CurrentRevision, long IncidentRevision, DateTime ServerUtc);

public record DiaryEntryCanceledResponse(Guid EntryId, int CurrentRevision, bool Canceled, long IncidentRevision, DateTime ServerUtc);

public record IncidentChangedEvent(Guid IncidentId, long Revision, DateTime ServerUtc);

public record InitialLeaseChangedEvent(Guid IncidentId, string LeaseOwner, DateTime ExpiresAtUtc, DateTime ServerUtc);

public record LeaseChangedEvent(Guid IncidentId, string? LeaseOwner, string? LeaseSessionId, DateTime? ExpiresAtUtc, DateTime ServerUtc);

public record MapElementChangedEvent(Guid IncidentId, long IncidentRevision, Guid ElementId, string Action, DateTime ServerUtc);

public record DiaryEntryChangedEvent(Guid IncidentId, Guid EntryId, long EntryNumber, string Action, long IncidentRevision, DateTime ServerUtc);

public record MapHydrantDto(int Id, double Longitude, double Latitude, HydrantStatus Status, int NominalDiameter);

public record MapStateResponse(Guid IncidentId, IncidentStatus IncidentStatus, long Revision, DateTime ServerUtc, EditorLeaseDto? Lease, IReadOnlyCollection<MapElementDto> Elements, IReadOnlyCollection<MapHydrantDto> Hydrants);
