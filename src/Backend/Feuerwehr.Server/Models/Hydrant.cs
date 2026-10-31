using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Net;
using System.Text;
using De.Openelp.Feuerwehr.Common.Models;

namespace De.Openelp.Feuerwehr.Server.Models
{

    public class Hydrant
    {
        [Key]
        public int Id { get; set; }

        public Address Address { get; set; } = new();

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
        public bool IsUnderground => Type == HydrantType.Underground;
        public bool IsOperational => Status == HydrantStatus.Operational;

        public string DiameterDescription => $"DIN {NominalDiameter}";
    }
}
