namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Carries the result of inventory import operations.
    /// </summary>
    /// <param name="CreatedCount">The number of records created by the import.</param>
    /// <param name="UpdatedCount">The number of existing records updated by the import.</param>
    /// <param name="Errors">The validation errors found while importing the file.</param>
    public sealed record InventoryImportResponse(int CreatedCount, int UpdatedCount, IReadOnlyList<InventoryImportError> Errors);
}
