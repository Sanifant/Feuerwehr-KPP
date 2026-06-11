using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Net;
using System.Text;

namespace Feuerwehr.Common.Models
{

    public class Address
    {
        public string Street { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? AdditionalInfo { get; set; }

        public string GetFormattedAddress() =>
            $"{Street} {HouseNumber}, {PostalCode} {City}".Trim();
    }

    public class Hydrant
    {
        [Key]
        public int Id { get; set; }

        public Address Address { get; set; } = new();

        [Required]
        public int Latitude { get; set; }

        [Required]
        public int Longitude { get; set; }

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
