using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    public sealed class DecideVehicleReservationRequest
    {
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
    }
}
