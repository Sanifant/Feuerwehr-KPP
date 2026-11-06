using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.Vehicles
{
    public sealed class SaveVehicleTypeRequest
    {
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
    }
}
