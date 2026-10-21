namespace de.openelp.feuerwehr.Common.Models;

public sealed record HydrantImportResponse(int ImportedCount, IReadOnlyList<HydrantImportError> Errors);

public sealed record HydrantImportError(long? LineNumber, string Description);
