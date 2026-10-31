namespace De.Openelp.Feuerwehr.Common.Models
{
    /// <summary>
    /// Executes hydrant Import Response.
    /// </summary>
    /// <param name="ImportedCount">The imported Count.</param>
    /// <param name="Errors">The errors.</param>
    public sealed record HydrantImportResponse(int ImportedCount, IReadOnlyList<HydrantImportError> Errors);
}
