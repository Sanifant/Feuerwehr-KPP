using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles;

[JsonConverter(typeof(JsonStringEnumConverter<VehicleReservationStatus>))]
public enum VehicleReservationStatus { Pending, Confirmed, Rejected, Cancelled }

[JsonConverter(typeof(JsonStringEnumConverter<VehicleDayStatus>))]
public enum VehicleDayStatus { Free, Pending, Booked, Unavailable }

/// <summary>
/// Executes vehicle Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="FireDepartmentId">The fire Department Id.</param>
/// <param name="FireDepartmentName">The fire Department Name.</param>
/// <param name="RadioCallSign">The radio Call Sign.</param>
/// <param name="Name">The name.</param>
/// <param name="LicensePlate">The license Plate.</param>
/// <param name="EquipmentOfficerId">The equipment Officer Id.</param>
/// <param name="EquipmentOfficerName">The equipment Officer Name.</param>
/// <param name="IsActive">The is Active.</param>
/// <param name="Version">The version.</param>
/// <param name="CanEdit">The can Edit.</param>
/// <param name="CanBookDirectly">The can Book Directly.</param>
/// <param name="VehicleTypeId">The vehicle Type Id.</param>
/// <param name="VehicleTypeName">The vehicle Type Name.</param>
/// <param name="VehicleStatusId">The vehicle Status Id.</param>
/// <param name="VehicleStatusName">The vehicle Status Name.</param>
/// <param name="AllowsBooking">The allows Booking.</param>
/// <param name="StatusChangedAtUtc">The status Changed At Utc.</param>
public sealed record VehicleDto(Guid Id, int FireDepartmentId, string FireDepartmentName, string RadioCallSign,
    string? Name, string? LicensePlate, string EquipmentOfficerId, string EquipmentOfficerName,
    bool IsActive, int Version, bool CanEdit, bool CanBookDirectly, Guid? VehicleTypeId = null, string? VehicleTypeName = null,
    Guid VehicleStatusId = default, string VehicleStatusName = "Betriebsbereit", bool AllowsBooking = true, DateTime? StatusChangedAtUtc = null);
/// <summary>
/// Executes vehicle Type Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
/// <param name="IsActive">The is Active.</param>
/// <param name="Version">The version.</param>
public sealed record VehicleTypeDto(Guid Id, string Name, bool IsActive, int Version);
/// <summary>
/// Executes vehicle Status Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
/// <param name="AllowsBooking">The allows Booking.</param>
/// <param name="IsActive">The is Active.</param>
/// <param name="Version">The version.</param>
/// <param name="IsDefault">The is Default.</param>
public sealed record VehicleStatusDto(Guid Id, string Name, bool AllowsBooking, bool IsActive, int Version, bool IsDefault);
/// <summary>
/// Executes vehicle Day Dto.
/// </summary>
/// <param name="Date">The date.</param>
/// <param name="Status">The status.</param>
public sealed record VehicleDayDto(DateOnly Date, VehicleDayStatus Status);
/// <summary>
/// Executes vehicle Availability Dto.
/// </summary>
/// <param name="Vehicle">The vehicle.</param>
/// <param name="Days">The days.</param>
public sealed record VehicleAvailabilityDto(VehicleDto Vehicle, IReadOnlyList<VehicleDayDto> Days);
/// <summary>
/// Executes vehicle Officer Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
/// <param name="FireDepartmentId">The fire Department Id.</param>
public sealed record VehicleOfficerDto(string Id, string Name, int FireDepartmentId);
/// <summary>
/// Executes vehicle Department Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
public sealed record VehicleDepartmentDto(int Id, string Name);
/// <summary>
/// Executes vehicle Management Options Dto.
/// </summary>
/// <param name="Departments">The departments.</param>
/// <param name="Officers">The officers.</param>
/// <param name="DefaultStatusId">The default Status Id.</param>
public sealed record VehicleManagementOptionsDto(IReadOnlyList<VehicleDepartmentDto> Departments, IReadOnlyList<VehicleOfficerDto> Officers, Guid DefaultStatusId = default);
/// <summary>
/// Executes vehicle Reservation Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="VehicleId">The vehicle Id.</param>
/// <param name="RadioCallSign">The radio Call Sign.</param>
/// <param name="FireDepartmentName">The fire Department Name.</param>
/// <param name="From">The from.</param>
/// <param name="To">The to.</param>
/// <param name="Comment">The comment.</param>
/// <param name="Status">The status.</param>
/// <param name="RequesterName">The requester Name.</param>
/// <param name="CreatedAtUtc">The created At Utc.</param>
/// <param name="DecidedAtUtc">The decided At Utc.</param>
/// <param name="IsDirectBooking">The is Direct Booking.</param>
/// <param name="Version">The version.</param>
/// <param name="CanDecide">The can Decide.</param>
/// <param name="CanCancel">The can Cancel.</param>
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

