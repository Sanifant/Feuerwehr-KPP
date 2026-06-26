namespace Feuerwehr.Server.Models;


public enum TrainingLevel
{
    Municipality = 1,
    District = 2,
    State = 3
}

public enum TrainingStatus
{
    Planned = 1,
    Open = 2,
    FullyBooked = 3,
    Completed = 4,
    Cancelled = 5
}

public class TrainingCourse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TrainingLevel Level { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Location { get; set; } = string.Empty;

    public int MaxParticipants { get; set; }

    public int AvailableSlots { get; set; }

    public TrainingStatus Status { get; set; }

    public int? OrganizingFireDepartmentId { get; set; }

    public FireDepartment? OrganizingFireDepartment { get; set; }
}