using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Auth
{
    /// <summary>
    /// Specifies the input for update user operations.
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        [EmailAddress]
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the user's given name.
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the user's family name.
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the associated fire department.
        /// </summary>
        public int? FireDepartmentId { get; set; }

        // Null continues to mean "keep the current assignment" for existing clients.
        /// <summary>
        /// Gets or sets a value indicating whether the user's existing fire-department assignment should be removed.
        /// </summary>
        public bool ClearFireDepartment { get; set; }

        /// <summary>
        /// Gets or sets the update user is active.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Gets or sets the Identity roles assigned to the user.
        /// </summary>
        public List<string>? Roles { get; set; }

        /// <summary>
        /// Gets or sets the new password to validate and assign to the account.
        /// </summary>
        public string? NewPassword { get; set; }
    }
}
