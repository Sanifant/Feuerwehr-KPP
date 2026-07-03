namespace Feuerwehr.Server.Models;

public class FireDepartmentTrainingCourse
{
    public int FireDepartmentId { get; set; }

    public FireDepartment FireDepartment { get; set; } = null!;

    public int TrainingCourseId { get; set; }

    public TrainingCourse TrainingCourse { get; set; } = null!;

    public int SeatsAssigned { get; set; }
}
