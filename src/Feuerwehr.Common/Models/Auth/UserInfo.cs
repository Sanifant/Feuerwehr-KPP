namespace De.Openelp.Feuerwehr.Common.Models.Auth
{
    /// <summary>
    /// Carries the authenticated user's profile and role information in the login response.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier of the user info.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's given name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's family name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's combined given and family names.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the associated fire department.
        /// </summary>
        public int? FireDepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the Identity roles assigned to the user.
        /// </summary>
        public List<string> Roles { get; set; } = new ();
    }
}
