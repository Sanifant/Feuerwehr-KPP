namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries fire department data across the API boundary.
    /// </summary>
    public class FireDepartmentDto
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
        /// Gets or sets the training courses associated with this fire department.
        /// </summary>
        public ICollection<FireDepartmentTrainingCourseDto> TrainingCourses { get; set; } = new List<FireDepartmentTrainingCourseDto>();
    }
}
