using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.Vehicles
{
    public sealed class SaveVehicleStatusRequest
    {
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        public bool AllowsBooking { get; set; }
        public bool IsActive { get; set; } = true;
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
    }
}
