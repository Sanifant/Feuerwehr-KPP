using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Auth
{
    /// <summary>
    /// Specifies the input for register user operations.
    /// </summary>
    public class RegisterUserRequest
    {
        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's given name.
        /// </summary>
        [Required]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's family name.
        /// </summary>
        [Required]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Identity roles assigned to the user.
        /// </summary>
        [Required]
        public List<string> Roles { get; set; } = new ();

        /// <summary>
        /// Gets or sets the identifier of the associated fire department.
        /// </summary>
        public int? FireDepartmentId { get; set; }
    }
}
