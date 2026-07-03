namespace Feuerwehr.Server.Models;

public class FireDepartment
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ContactPersonName { get; set; }

    public string? ContactPersonEmail { get; set; }

    public ICollection<FireDepartmentTrainingCourse> FireDepartmentTrainingCourses { get; set; } = new List<FireDepartmentTrainingCourse>();
}
