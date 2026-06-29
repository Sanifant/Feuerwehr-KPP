namespace Feuerwehr.Common.Models.Dto;

public class FireDepartmentDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Municipality { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string? ContactPersonName { get; set; }

    public string? ContactPersonEmail { get; set; }
}