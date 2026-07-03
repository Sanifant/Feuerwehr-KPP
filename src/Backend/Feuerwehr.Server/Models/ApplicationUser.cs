using Microsoft.AspNetCore.Identity;

namespace Feuerwehr.Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? FireDepartmentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation property
        public FireDepartment? FireDepartment { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
