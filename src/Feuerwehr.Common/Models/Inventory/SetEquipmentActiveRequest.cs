using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    public sealed class SetEquipmentActiveRequest
    {
        public bool IsActive { get; set; }
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
    }
}
