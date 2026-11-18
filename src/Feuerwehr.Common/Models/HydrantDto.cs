using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models
{
    public class HydrantDto
    {
        public int Id { get; set; }

        public AddressDto Address { get; set; } = new ();

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        public int NominalDiameter { get; set; }

        public HydrantType Type { get; set; }

        public WaterSource WaterSource { get; set; }

        public HydrantStatus Status { get; set; }

        public string? Notes { get; set; }

        // Computed Read-Only Properties (Excluded from Database Mapping)
        public bool IsUnderground => this.Type == HydrantType.Underground;

        public bool IsOperational => this.Status == HydrantStatus.Operational;

        public string DiameterDescription => $"DIN {this.NominalDiameter}";
    }
}
