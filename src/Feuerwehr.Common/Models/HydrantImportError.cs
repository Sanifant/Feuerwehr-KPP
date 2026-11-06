namespace De.Openelp.Feuerwehr.Common.Models
{
    /// <summary>
    /// Describes a hydrant import validation failure and its optional source line.
    /// </summary>
    /// <param name="LineNumber">The source-file line associated with the error, or null for a file-level error.</param>
    /// <param name="Description">The descriptive text for the hydrant import error.</param>
    public sealed record HydrantImportError(long? LineNumber, string Description);
}
