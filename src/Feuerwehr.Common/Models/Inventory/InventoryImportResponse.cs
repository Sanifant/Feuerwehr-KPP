using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory;

/// <summary>
/// Executes inventory Import Response.
/// </summary>
/// <param name="CreatedCount">The created Count.</param>
/// <param name="UpdatedCount">The updated Count.</param>
/// <param name="Errors">The errors.</param>
public sealed record InventoryImportResponse(int CreatedCount, int UpdatedCount, IReadOnlyList<InventoryImportError> Errors);
