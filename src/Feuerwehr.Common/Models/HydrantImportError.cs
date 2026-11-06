namespace de.openelp.feuerwehr.Common.Models
{
    /// <summary>
    /// Executes hydrant Import Error.
    /// </summary>
    /// <param name="LineNumber">The line Number.</param>
    /// <param name="Description">The description.</param>
    public sealed record HydrantImportError(long? LineNumber, string Description);
}
