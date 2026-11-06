namespace De.Openelp.Feuerwehr.Server.Models.Inventory
{
    /// <summary>
    /// Groups equipment types and defines the category code used to allocate barcodes.
    /// </summary>
    public sealed class InventoryCategory
    {
        /// <summary>
        /// Gets or sets the unique identifier of the inventory category.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the inventory category.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the normalized name used to compare and uniquely identify the inventory category.
        /// </summary>
        public string NormalizedName { get; set; } = string.Empty;
    }
}
