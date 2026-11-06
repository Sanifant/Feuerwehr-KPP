namespace de.openelp.feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Executes inventory Import Error.
    /// </summary>
    /// <param name="LineNumber">The line Number.</param>
    /// <param name="Description">The description.</param>
    public sealed record InventoryImportError(long? LineNumber, string Description);
}
