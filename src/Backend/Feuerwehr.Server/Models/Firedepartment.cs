namespace De.Openelp.Feuerwehr.Server.Models
{
    /// <summary>
    /// Describes a fire department and its contact information.
    /// </summary>
    public class FireDepartment
    {
        /// <summary>
        /// Gets or sets the unique identifier of the fire department.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the fire department.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the fire department contact person's name.
        /// </summary>
        public string? ContactPersonName { get; set; }

        /// <summary>
        /// Gets or sets the fire department contact person's email address.
        /// </summary>
        public string? ContactPersonEmail { get; set; }

        /// <summary>
        /// Gets or sets the fire department training courses associated with this fire department.
        /// </summary>
        public ICollection<FireDepartmentTrainingCourse> FireDepartmentTrainingCourses { get; set; } = new List<FireDepartmentTrainingCourse>();
    }
}
