namespace De.Openelp.Feuerwehr.Server.Models.Inventory
{
    public sealed class InventoryCategory
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string NormalizedName { get; set; } = string.Empty;
    }
}
