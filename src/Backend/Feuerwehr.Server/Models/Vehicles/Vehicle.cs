namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    /// <summary>
    /// Stores vehicle details, department and officer assignments, type, status, and activation state.
    /// </summary>
    public sealed class Vehicle
    {
        /// <summary>
        /// Gets or sets the unique identifier of the vehicle.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the associated fire department.
        /// </summary>
        public int FireDepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the associated fire department.
        /// </summary>
        public FireDepartment FireDepartment { get; set; } = null!;

        /// <summary>
        /// Gets or sets the vehicle's radio call sign.
        /// </summary>
        public string RadioCallSign { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the vehicle.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the vehicle registration plate.
        /// </summary>
        public string? LicensePlate { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the vehicle type.
        /// </summary>
        public Guid? VehicleTypeId { get; set; }

        /// <summary>
        /// Gets or sets the associated vehicle type.
        /// </summary>
        public VehicleType? VehicleType { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the vehicle's configured status.
        /// </summary>
        public Guid VehicleStatusId { get; set; } = VehicleStatusDefaults.ReadyId;

        /// <summary>
        /// Gets or sets the vehicle's configured operational status.
        /// </summary>
        public VehicleStatus VehicleStatus { get; set; } = null!;

        /// <summary>
        /// Gets or sets the UTC timestamp of status changed for this vehicle.
        /// </summary>
        public DateTime? StatusChangedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the assigned equipment officer.
        /// </summary>
        public string EquipmentOfficerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user assigned as equipment officer for the vehicle.
        /// </summary>
        public ApplicationUser EquipmentOfficer { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether the vehicle is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the concurrency version used to detect conflicting updates.
        /// </summary>
        public int Version { get; set; } = 1;
    }
}
