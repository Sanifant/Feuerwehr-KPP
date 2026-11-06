namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries training course data across the API boundary.
    /// </summary>
    public class TrainingCourseDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the training course.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the title displayed for the training course.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the descriptive text for the training course.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the training qualification level.
        /// </summary>
        public TrainingLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the current state of the training course.
        /// </summary>
        public TrainingStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the department assigned to the vehicle.
        /// </summary>
        public int AssignedFireDepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the name of the department assigned to the vehicle.
        /// </summary>
        public string AssignedFireDepartmentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of seats assigned to the vehicle.
        /// </summary>
        public int AssignedSeats { get; set; }
    }
}
