using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Auth;

public class UpdateUserRequest
    {
        [EmailAddress]
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? FireDepartmentId { get; set; }
        // Null continues to mean "keep the current assignment" for existing clients.
        public bool ClearFireDepartment { get; set; }
        public bool? IsActive { get; set; }
        public List<string>? Roles { get; set; }
        public string? NewPassword { get; set; }
    }

