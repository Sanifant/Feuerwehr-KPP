using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    public sealed class SaveVehicleTypeRequest
    {
        [Required, StringLength(100)] public string Name { get; set; } = "";
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; } = 1;
    }
}
