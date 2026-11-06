using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Carries vehicle import data across the API boundary.
    /// </summary>
    public sealed class VehicleImportDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the vehicle import.
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Gets or sets the vehicle's radio call sign.
        /// </summary>
        [Required, StringLength(100)] public string RadioCallSign { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the fire department name used when matching imported vehicle assignments.
        /// </summary>
        [Required, StringLength(300)] public string FireDepartment { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the associated fire department.
        /// </summary>
        [Range(1, int.MaxValue)] public int? FireDepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the email address used to identify the assigned equipment officer during transfer.
        /// </summary>
        [Required, EmailAddress] public string EquipmentOfficerEmail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the vehicle import.
        /// </summary>
        [StringLength(150)] public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the vehicle registration plate.
        /// </summary>
        [StringLength(30)] public string? LicensePlate { get; set; }

        /// <summary>
        /// Gets or sets the vehicle type name used for import matching.
        /// </summary>
        [StringLength(100)] public string? VehicleType { get; set; }

        /// <summary>
        /// Gets or sets the current state of the vehicle import.
        /// </summary>
        [StringLength(100)] public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the vehicle import is active.
        /// </summary>
        public bool? IsActive { get; set; }
    }
}
