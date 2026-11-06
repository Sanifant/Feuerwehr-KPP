namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record UserSummaryDto(string Id, string? Email, string FirstName, string LastName, string FullName, int? FireDepartmentId, bool IsActive, IReadOnlyCollection<string> Roles, bool PasswordResetRequired = false);
}
