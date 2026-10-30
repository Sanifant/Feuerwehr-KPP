using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles;

/// <summary>
/// Executes vehicle Import Error.
/// </summary>
/// <param name="LineNumber">The line Number.</param>
/// <param name="Description">The description.</param>
public sealed record VehicleImportError(long? LineNumber, string Description);
/// <summary>
/// Executes vehicle Import Response.
/// </summary>
/// <param name="CreatedCount">The created Count.</param>
/// <param name="UpdatedCount">The updated Count.</param>
/// <param name="Errors">The errors.</param>
public sealed record VehicleImportResponse(int CreatedCount, int UpdatedCount, IReadOnlyList<VehicleImportError> Errors);

public sealed class VehicleTypeImportDto
{
    public Guid? Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class VehicleImportDto
{
    public Guid? Id { get; set; }
    [Required, StringLength(100)] public string RadioCallSign { get; set; } = "";
    [Required, StringLength(300)] public string FireDepartment { get; set; } = "";
    [Range(1, int.MaxValue)] public int? FireDepartmentId { get; set; }
    [Required, EmailAddress] public string EquipmentOfficerEmail { get; set; } = "";
    [StringLength(150)] public string? Name { get; set; }
    [StringLength(30)] public string? LicensePlate { get; set; }
    [StringLength(100)] public string? VehicleType { get; set; }
    [StringLength(100)] public string? Status { get; set; }
    public bool? IsActive { get; set; }
}

