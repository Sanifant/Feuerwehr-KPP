namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Carries inventory category data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the inventory category.</param>
    /// <param name="Name">The display name of the inventory category.</param>
    public sealed record InventoryCategoryDto(Guid Id, string Name);
}
