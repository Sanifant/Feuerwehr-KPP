using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles;

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
