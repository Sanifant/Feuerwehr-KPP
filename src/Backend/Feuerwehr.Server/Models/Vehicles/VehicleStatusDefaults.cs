namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    /// <summary>
    /// Defines stable identifiers for the seeded ready, maintenance, and deregistered vehicle statuses.
    /// </summary>
    public static class VehicleStatusDefaults
    {
        /// <summary>
        /// The the identifier of the associated ready.
        /// </summary>
        public static readonly Guid ReadyId = Guid.Parse("5032f99d-4352-4000-8000-000000000001");
        /// <summary>
        /// The the identifier of the associated maintenance.
        /// </summary>
        public static readonly Guid MaintenanceId = Guid.Parse("5032f99d-4352-4000-8000-000000000002");
        /// <summary>
        /// The the identifier of the associated deregistered.
        /// </summary>
        public static readonly Guid DeregisteredId = Guid.Parse("5032f99d-4352-4000-8000-000000000003");
    }
}
