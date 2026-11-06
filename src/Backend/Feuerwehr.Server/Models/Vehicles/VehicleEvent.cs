namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    /// <summary>
    /// Stores an audit event for a vehicle or reservation change.
    /// </summary>
    public sealed class VehicleEvent
    {
        /// <summary>
        /// Gets or sets the unique identifier of the vehicle event.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the vehicle.
        /// </summary>
        public Guid VehicleId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the vehicle reservation.
        /// </summary>
        public Guid? ReservationId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who performed the audited change.
        /// </summary>
        public string ActorId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the operation name recorded in the audit or live event.
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the details recorded with the vehicle audit event.
        /// </summary>
        public string Details { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC time at which the audited change occurred.
        /// </summary>
        public DateTime OccurredAtUtc { get; set; }
    }
}
