namespace Feuerwehr.Server.Models;

public class FireDepartment
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Municipality { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string? ContactPersonName { get; set; }

    public string? ContactPersonEmail { get; set; }

    public ICollection<TrainingCourse> TrainingCourses { get; set; } = new List<TrainingCourse>();
}