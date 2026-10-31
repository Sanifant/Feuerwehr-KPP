using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.Server.Models.Inventory
{
    public sealed class InventoryCategory
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";
    }
}
