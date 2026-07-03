namespace Feuerwehr.Common.Models.Dto;

public class FireDepartmentDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ContactPersonName { get; set; }

    public string? ContactPersonEmail { get; set; }

    public ICollection<FireDepartmentTrainingCourseDto> TrainingCourses { get; set; } = new List<FireDepartmentTrainingCourseDto>();
}
