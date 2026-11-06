using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.Common.Models.Inventory
{
    public sealed class SetEquipmentActiveRequest
    {
        public bool IsActive { get; set; }
        [Range(1, int.MaxValue)] public int ExpectedVersion { get; set; }
    }
}
