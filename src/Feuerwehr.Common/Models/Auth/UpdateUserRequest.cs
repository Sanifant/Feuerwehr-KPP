using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.Auth;

public class UpdateUserRequest
    {
        [EmailAddress]
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? FireDepartmentId { get; set; }
        public bool? IsActive { get; set; }
        public List<string>? Roles { get; set; }
        public string? NewPassword { get; set; }
    }
