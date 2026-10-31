using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto;

public record UserDetailsDto(string Id, string? Email, string FirstName, string LastName, string FullName, int? FireDepartmentId, bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt, IReadOnlyCollection<string> Roles, bool PasswordResetRequired = false);
