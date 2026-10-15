using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.Auth
{
    public class RegisterUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public List<string> Roles { get; set; } = new();

        public int? FireDepartmentId { get; set; }
    }
}
