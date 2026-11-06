namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Describes an inventory import validation failure and its optional source line.
    /// </summary>
    /// <param name="LineNumber">The source-file line associated with the error, or null for a file-level error.</param>
    /// <param name="Description">The descriptive text for the inventory import error.</param>
    public sealed record InventoryImportError(long? LineNumber, string Description);
}
