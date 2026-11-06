using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.Vehicles
{
    public sealed class VehicleImportDto
    {
        public Guid? Id { get; set; }
        [Required, StringLength(100)] public string RadioCallSign { get; set; } = string.Empty;
        [Required, StringLength(300)] public string FireDepartment { get; set; } = string.Empty;
        [Range(1, int.MaxValue)] public int? FireDepartmentId { get; set; }
        [Required, EmailAddress] public string EquipmentOfficerEmail { get; set; } = string.Empty;
        [StringLength(150)] public string? Name { get; set; }
        [StringLength(30)] public string? LicensePlate { get; set; }
        [StringLength(100)] public string? VehicleType { get; set; }
        [StringLength(100)] public string? Status { get; set; }
        public bool? IsActive { get; set; }
    }
}
