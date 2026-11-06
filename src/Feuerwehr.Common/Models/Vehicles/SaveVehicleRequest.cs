using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Specifies the input for save vehicle operations.
    /// </summary>
    public sealed class SaveVehicleRequest
    {
        /// <summary>
        /// Gets or sets the identifier of the associated fire department.
        /// </summary>
        [Range(1, int.MaxValue)] public int FireDepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the vehicle's radio call sign.
        /// </summary>
        [Required, StringLength(100)] public string RadioCallSign { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the save vehicle.
        /// </summary>
        [StringLength(150)] public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the vehicle registration plate.
        /// </summary>
        [StringLength(30)] public string? LicensePlate { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the vehicle type.
        /// </summary>
        public Guid? VehicleTypeId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the vehicle's configured status.
        /// </summary>
        public Guid? VehicleStatusId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the assigned equipment officer.
        /// </summary>
        [Required] public string EquipmentOfficerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version the caller last read; a mismatch rejects the update.
        /// </summary>
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
    }
}
