using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace de.openelp.feuerwehr.Common.Models.Vehicles;

[JsonConverter(typeof(JsonStringEnumConverter<VehicleReservationStatus>))]
public enum VehicleReservationStatus { Pending, Confirmed, Rejected, Cancelled }

[JsonConverter(typeof(JsonStringEnumConverter<VehicleDayStatus>))]
public enum VehicleDayStatus { Free, Pending, Booked, Unavailable }

public sealed record VehicleDto(Guid Id, int FireDepartmentId, string FireDepartmentName, string RadioCallSign,
    string? Name, string? LicensePlate, string EquipmentOfficerId, string EquipmentOfficerName,
    bool IsActive, int Version, bool CanEdit, bool CanBookDirectly, Guid? VehicleTypeId = null, string? VehicleTypeName = null,
    Guid VehicleStatusId = default, string VehicleStatusName = "Betriebsbereit", bool AllowsBooking = true, DateTime? StatusChangedAtUtc = null);
public sealed record VehicleTypeDto(Guid Id, string Name, bool IsActive, int Version);
public sealed record VehicleStatusDto(Guid Id, string Name, bool AllowsBooking, bool IsActive, int Version, bool IsDefault);
public sealed record VehicleDayDto(DateOnly Date, VehicleDayStatus Status);
public sealed record VehicleAvailabilityDto(VehicleDto Vehicle, IReadOnlyList<VehicleDayDto> Days);
public sealed record VehicleOfficerDto(string Id, string Name, int FireDepartmentId);
public sealed record VehicleDepartmentDto(int Id, string Name);
public sealed record VehicleManagementOptionsDto(IReadOnlyList<VehicleDepartmentDto> Departments, IReadOnlyList<VehicleOfficerDto> Officers, Guid DefaultStatusId = default);
public sealed record VehicleReservationDto(Guid Id, Guid VehicleId, string RadioCallSign, string FireDepartmentName,
    DateOnly From, DateOnly To, string? Comment, VehicleReservationStatus Status, string RequesterName,
    DateTime CreatedAtUtc, DateTime? DecidedAtUtc, bool IsDirectBooking, int Version, bool CanDecide, bool CanCancel);

public sealed class SaveVehicleRequest
{
    [Range(1, int.MaxValue)] public int FireDepartmentId { get; set; }
    [Required, StringLength(100)] public string RadioCallSign { get; set; } = "";
    [StringLength(150)] public string? Name { get; set; }
    [StringLength(30)] public string? LicensePlate { get; set; }
    public Guid? VehicleTypeId { get; set; }
    public Guid? VehicleStatusId { get; set; }
    [Required] public string EquipmentOfficerId { get; set; } = "";
    [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
}

public sealed class SaveVehicleTypeRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
}

public sealed class SetVehicleTypeActiveRequest
{
    public bool IsActive { get; set; }
    [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
}

public sealed class SaveVehicleStatusRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    public bool AllowsBooking { get; set; }
    public bool IsActive { get; set; } = true;
    [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
}

public sealed class SetVehicleActiveRequest
{
    public bool IsActive { get; set; }
    [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
}

public sealed class CreateVehicleReservationRequest
{
    public Guid CommandId { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    [StringLength(2000)] public string? Comment { get; set; }
}

public sealed class DecideVehicleReservationRequest
{
    [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
}
