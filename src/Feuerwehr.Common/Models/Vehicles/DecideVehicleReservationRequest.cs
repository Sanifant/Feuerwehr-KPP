using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    public sealed class DecideVehicleReservationRequest
    {
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
    }
}
