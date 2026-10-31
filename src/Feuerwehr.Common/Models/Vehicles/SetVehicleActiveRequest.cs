using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles;

public sealed class SetVehicleActiveRequest
{
    public bool IsActive { get; set; }
    [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
}
