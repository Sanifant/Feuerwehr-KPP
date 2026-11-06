using Microsoft.AspNetCore.Identity;

namespace De.Openelp.Feuerwehr.Server.Models
{
    /// <summary>
    /// Extends the Identity user with firefighter details, department membership, and account activation state.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Gets or sets the user's given name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's family name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the associated fire department.
        /// </summary>
        public int? FireDepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the date and time the user account was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the time of the user's most recent successful login.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the application user is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the account must complete password assignment before signing in.
        /// </summary>
        public bool PasswordResetRequired { get; set; }

        // Navigation property
        /// <summary>
        /// Gets or sets the associated fire department.
        /// </summary>
        public FireDepartment? FireDepartment { get; set; }

        /// <summary>
        /// Gets the user's combined given and family names.
        /// </summary>
        public string FullName => $"{this.FirstName} {this.LastName}".Trim();
    }
}
