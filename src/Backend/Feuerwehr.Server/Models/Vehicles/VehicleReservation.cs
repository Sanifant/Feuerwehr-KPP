using De.Openelp.Feuerwehr.Common.Models.Vehicles;

namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    /// <summary>
    /// Stores a vehicle booking request or confirmed reservation, its date range, and decision history.
    /// </summary>
    public sealed class VehicleReservation
    {
        /// <summary>
        /// Gets or sets the unique identifier of the vehicle reservation.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the vehicle.
        /// </summary>
        public Guid VehicleId { get; set; }

        /// <summary>
        /// Gets or sets the vehicle associated with this vehicle reservation.
        /// </summary>
        public Vehicle Vehicle { get; set; } = null!;

        /// <summary>
        /// Gets or sets the client-generated command identifier used to detect retries.
        /// </summary>
        public Guid CommandId { get; set; }

        /// <summary>
        /// Gets or sets the hash of the command payload used to detect inconsistent retries.
        /// </summary>
        public string PayloadHash { get; set; } = string.Empty;

        // Snapshot identifiers and names preserve history when an account is removed or renamed.
        /// <summary>
        /// Gets or sets the identifier of the user who requested the reservation.
        /// </summary>
        public string RequesterId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the requesting user's display name at booking time.
        /// </summary>
        public string RequesterName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the requesting user's email address at booking time.
        /// </summary>
        public string RequesterEmail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the vehicle's radio call sign.
        /// </summary>
        public string RadioCallSign { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the associated fire department's display name.
        /// </summary>
        public string FireDepartmentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the first date of the reservation range, inclusive.
        /// </summary>
        public DateOnly From { get; set; }

        /// <summary>
        /// Gets or sets the last date of the reservation range, inclusive.
        /// </summary>
        public DateOnly To { get; set; }

        /// <summary>
        /// Gets or sets the comment recorded with the vehicle reservation.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets the current state of the vehicle reservation.
        /// </summary>
        public VehicleReservationStatus Status { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the reservation was booked directly rather than submitted for approval.
        /// </summary>
        public bool IsDirectBooking { get; set; }

        /// <summary>
        /// Gets or sets the UTC time at which the record was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC time of the reservation decision.
        /// </summary>
        public DateTime? DecidedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who decided the reservation request.
        /// </summary>
        public string? DecidedById { get; set; }

        /// <summary>
        /// Gets or sets the concurrency version used to detect conflicting updates.
        /// </summary>
        public int Version { get; set; } = 1;
    }
}
