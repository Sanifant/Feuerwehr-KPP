namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries fire department training course data across the API boundary.
    /// </summary>
    public class FireDepartmentTrainingCourseDto
    {
        /// <summary>
        /// Gets or sets the identifier of the associated training course.
        /// </summary>
        public int TrainingCourseId { get; set; }

        /// <summary>
        /// Gets or sets the title of the associated training course.
        /// </summary>
        public string TrainingCourseTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of seats assigned to the vehicle.
        /// </summary>
        public int AssignedSeats { get; set; }
    }
}
