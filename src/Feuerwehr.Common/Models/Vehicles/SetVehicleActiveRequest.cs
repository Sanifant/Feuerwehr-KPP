using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.Vehicles
{
    public sealed class SetVehicleActiveRequest
    {
        public bool IsActive { get; set; }
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
    }
}
