namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    /// <summary>
    /// Defines a configurable vehicle condition and whether that condition allows booking.
    /// </summary>
    public sealed class VehicleStatus
    {
        /// <summary>
        /// Gets or sets the unique identifier of the vehicle status.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the vehicle status.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the normalized name used to compare and uniquely identify the vehicle status.
        /// </summary>
        public string NormalizedName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether vehicles with this status can be reserved.
        /// </summary>
        public bool AllowsBooking { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the vehicle status is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the concurrency version used to detect conflicting updates.
        /// </summary>
        public int Version { get; set; } = 1;
    }
}
