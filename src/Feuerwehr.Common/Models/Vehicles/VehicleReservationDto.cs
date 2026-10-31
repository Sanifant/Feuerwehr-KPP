using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles;

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
