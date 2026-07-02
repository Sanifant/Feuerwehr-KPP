using Feuerwehr.Common.Models.Dto;

namespace Feuerwehr.Server.Models;

public class TrainingCourse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TrainingLevel Level { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public TrainingStatus Status { get; set; }

    public ICollection<FireDepartmentTrainingCourse> FireDepartmentTrainingCourses { get; set; } = new List<FireDepartmentTrainingCourse>();
}
