namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries user details data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the user details.</param>
    /// <param name="Email">The user's email address.</param>
    /// <param name="FirstName">The user's given name.</param>
    /// <param name="LastName">The user's family name.</param>
    /// <param name="FullName">The user's combined given and family names.</param>
    /// <param name="FireDepartmentId">The identifier of the associated fire department.</param>
    /// <param name="IsActive">The user details is active.</param>
    /// <param name="CreatedAt">The date and time the user account was created.</param>
    /// <param name="LastLoginAt">The time of the user's most recent successful login.</param>
    /// <param name="Roles">The Identity roles assigned to the user.</param>
    /// <param name="PasswordResetRequired">The account must complete password assignment before signing in.</param>
    public record UserDetailsDto(string Id, string? Email, string FirstName, string LastName, string FullName, int? FireDepartmentId, bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt, IReadOnlyCollection<string> Roles, bool PasswordResetRequired = false);
}
