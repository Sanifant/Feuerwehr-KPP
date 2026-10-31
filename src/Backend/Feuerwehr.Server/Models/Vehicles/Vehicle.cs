using De.Openelp.Feuerwehr.Common.Models.Vehicles;

namespace De.Openelp.Feuerwehr.Server.Models.Vehicles;

public sealed class Vehicle
{
    public Guid Id { get; set; }
    public int FireDepartmentId { get; set; }
    public FireDepartment FireDepartment { get; set; } = null!;
    public string RadioCallSign { get; set; } = "";
    public string? Name { get; set; }
    public string? LicensePlate { get; set; }
    public Guid? VehicleTypeId { get; set; }
    public VehicleType? VehicleType { get; set; }
    public Guid VehicleStatusId { get; set; } = VehicleStatusDefaults.ReadyId;
    public VehicleStatus VehicleStatus { get; set; } = null!;
    public DateTime? StatusChangedAtUtc { get; set; }
    public string EquipmentOfficerId { get; set; } = "";
    public ApplicationUser EquipmentOfficer { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
}
