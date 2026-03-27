using System;
using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.domain
{
    public class Hydrant
    {
        public Guid Id { get; set; }

        [Required]
        public string Number { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string Status { get; set; } = string.Empty;

        public string District { get; set; } = string.Empty;

        public Hydrant()
        {
            this.Id = Guid.NewGuid();
        }
    }
}
