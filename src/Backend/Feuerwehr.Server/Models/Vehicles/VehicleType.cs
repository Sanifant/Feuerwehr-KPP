namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    /// <summary>
    /// Defines a configurable vehicle type and its activation state.
    /// </summary>
    public sealed class VehicleType
    {
        /// <summary>
        /// Gets or sets the unique identifier of the vehicle type.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the vehicle type.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the normalized name used to compare and uniquely identify the vehicle type.
        /// </summary>
        public string NormalizedName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the vehicle type is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the concurrency version used to detect conflicting updates.
        /// </summary>
        public int Version { get; set; } = 1;
    }
}
