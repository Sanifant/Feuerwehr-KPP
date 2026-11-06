namespace De.Openelp.Feuerwehr.Server.Models
{
    /// <summary>
    /// Associates a fire department with a training course and its allocated seat count.
    /// </summary>
    public class FireDepartmentTrainingCourse
    {
        /// <summary>
        /// Gets or sets the identifier of the associated fire department.
        /// </summary>
        public int FireDepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the associated fire department.
        /// </summary>
        public FireDepartment FireDepartment { get; set; } = null!;

        /// <summary>
        /// Gets or sets the identifier of the associated training course.
        /// </summary>
        public int TrainingCourseId { get; set; }

        /// <summary>
        /// Gets or sets the associated training course.
        /// </summary>
        public TrainingCourse TrainingCourse { get; set; } = null!;

        /// <summary>
        /// Gets or sets the number of training-course seats assigned to the fire department.
        /// </summary>
        public int SeatsAssigned { get; set; }
    }
}
