namespace Feuerwehr.Common.Models.Dto;

public class FireDepartmentTrainingCourseDto
{
    public int TrainingCourseId { get; set; }

    public string TrainingCourseTitle { get; set; } = string.Empty;

    public int AssignedSeats { get; set; }
}
