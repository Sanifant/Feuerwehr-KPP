namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    /// <summary>
    /// Claims one calendar day for a confirmed vehicle reservation to prevent overlapping bookings.
    /// </summary>
    public sealed class VehicleBookingDay
    {
        /// <summary>
        /// Gets or sets the identifier of the vehicle.
        /// </summary>
        public Guid VehicleId { get; set; }

        /// <summary>
        /// Gets or sets the calendar date represented by this record.
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the vehicle reservation.
        /// </summary>
        public Guid ReservationId { get; set; }

        /// <summary>
        /// Gets or sets the reservation owning this booked calendar day.
        /// </summary>
        public VehicleReservation Reservation { get; set; } = null!;
    }
}
