using Microsoft.AspNetCore.Identity;

namespace De.Openelp.Feuerwehr.Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? FireDepartmentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool PasswordResetRequired { get; set; }

        // Navigation property
        public FireDepartment? FireDepartment { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}

