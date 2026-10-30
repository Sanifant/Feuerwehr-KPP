namespace De.Openelp.Feuerwehr.Common.Models;

/// <summary>
/// Executes hydrant Import Response.
/// </summary>
/// <param name="ImportedCount">The imported Count.</param>
/// <param name="Errors">The errors.</param>
public sealed record HydrantImportResponse(int ImportedCount, IReadOnlyList<HydrantImportError> Errors);

/// <summary>
/// Executes hydrant Import Error.
/// </summary>
/// <param name="LineNumber">The line Number.</param>
/// <param name="Description">The description.</param>
public sealed record HydrantImportError(long? LineNumber, string Description);

