using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    public sealed class SaveVehicleRequest
    {
        [Range(1, int.MaxValue)] public int FireDepartmentId { get; set; }
        [Required, StringLength(100)] public string RadioCallSign { get; set; } = "";
        [StringLength(150)] public string? Name { get; set; }
        [StringLength(30)] public string? LicensePlate { get; set; }
        public Guid? VehicleTypeId { get; set; }
        public Guid? VehicleStatusId { get; set; }
        [Required] public string EquipmentOfficerId { get; set; } = "";
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
    }
}
