namespace De.Openelp.Feuerwehr.Common.Models
{
    /// <summary>
    /// Carries the result of hydrant import operations.
    /// </summary>
    /// <param name="ImportedCount">The number of hydrants successfully imported.</param>
    /// <param name="Errors">The validation errors found while importing the file.</param>
    public sealed record HydrantImportResponse(int ImportedCount, IReadOnlyList<HydrantImportError> Errors);
}
