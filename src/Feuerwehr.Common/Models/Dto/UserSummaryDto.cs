namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries user summary data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the user summary.</param>
    /// <param name="Email">The user's email address.</param>
    /// <param name="FirstName">The user's given name.</param>
    /// <param name="LastName">The user's family name.</param>
    /// <param name="FullName">The user's combined given and family names.</param>
    /// <param name="FireDepartmentId">The identifier of the associated fire department.</param>
    /// <param name="IsActive">The user summary is active.</param>
    /// <param name="Roles">The Identity roles assigned to the user.</param>
    /// <param name="PasswordResetRequired">The account must complete password assignment before signing in.</param>
    public record UserSummaryDto(string Id, string? Email, string FirstName, string LastName, string FullName, int? FireDepartmentId, bool IsActive, IReadOnlyCollection<string> Roles, bool PasswordResetRequired = false);
}
