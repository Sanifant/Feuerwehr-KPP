namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Carries vehicle reservation data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the vehicle reservation.</param>
    /// <param name="VehicleId">The identifier of the vehicle.</param>
    /// <param name="RadioCallSign">The vehicle's radio call sign.</param>
    /// <param name="FireDepartmentName">The associated fire department's display name.</param>
    /// <param name="From">The first date of the reservation range, inclusive.</param>
    /// <param name="To">The last date of the reservation range, inclusive.</param>
    /// <param name="Comment">The comment recorded with the vehicle reservation.</param>
    /// <param name="Status">The current state of the vehicle reservation.</param>
    /// <param name="RequesterName">The requesting user's display name at booking time.</param>
    /// <param name="CreatedAtUtc">The UTC time at which the record was created.</param>
    /// <param name="DecidedAtUtc">The UTC time of the reservation decision.</param>
    /// <param name="IsDirectBooking">The reservation was booked directly rather than submitted for approval.</param>
    /// <param name="Version">The concurrency version used to detect conflicting updates.</param>
    /// <param name="CanDecide">The signed-in user can approve or reject this reservation.</param>
    /// <param name="CanCancel">The signed-in user can cancel this reservation.</param>
    public sealed record VehicleReservationDto(
        Guid Id,
        Guid VehicleId,
        string RadioCallSign,
        string FireDepartmentName,
        DateOnly From,
        DateOnly To,
        string? Comment,
        VehicleReservationStatus Status,
        string RequesterName,
        DateTime CreatedAtUtc,
        DateTime? DecidedAtUtc,
        bool IsDirectBooking,
        int Version,
        bool CanDecide,
        bool CanCancel);
}
