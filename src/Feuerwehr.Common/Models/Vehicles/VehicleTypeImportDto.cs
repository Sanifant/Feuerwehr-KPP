using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.Vehicles
{
    public sealed class VehicleTypeImportDto
    {
        public Guid? Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
