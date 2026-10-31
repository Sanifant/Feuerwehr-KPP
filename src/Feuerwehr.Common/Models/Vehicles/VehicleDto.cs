using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles;

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
