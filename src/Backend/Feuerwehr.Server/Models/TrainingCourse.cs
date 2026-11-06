using De.Openelp.Feuerwehr.Common.Models.Dto;

namespace De.Openelp.Feuerwehr.Server.Models
{
    /// <summary>
    /// Describes a training course, its qualification level, and scheduled dates.
    /// </summary>
    public class TrainingCourse
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
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the training qualification level.
        /// </summary>
        public TrainingLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the first date of the training course.
        /// </summary>
        public DateOnly StartDate { get; set; }

        /// <summary>
        /// Gets or sets the last date of the training course.
        /// </summary>
        public DateOnly EndDate { get; set; }

        /// <summary>
        /// Gets or sets the current state of the training course.
        /// </summary>
        public TrainingStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the fire department training courses associated with this training course.
        /// </summary>
        public ICollection<FireDepartmentTrainingCourse> FireDepartmentTrainingCourses { get; set; } = new List<FireDepartmentTrainingCourse>();
    }
}
