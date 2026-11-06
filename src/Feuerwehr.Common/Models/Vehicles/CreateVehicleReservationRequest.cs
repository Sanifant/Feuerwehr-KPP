using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    public sealed class CreateVehicleReservationRequest
    {
        public Guid CommandId { get; set; }

        public DateOnly From { get; set; }

        public DateOnly To { get; set; }

        [StringLength(2000)] public string? Comment { get; set; }
    }
}
