using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    public sealed class SaveVehicleStatusRequest
    {
        [Required, StringLength(100)] public string Name { get; set; } = "";
        public bool AllowsBooking { get; set; }
        public bool IsActive { get; set; } = true;
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
    }
}
