using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles;

/// <summary>
/// Executes vehicle Import Response.
/// </summary>
/// <param name="CreatedCount">The created Count.</param>
/// <param name="UpdatedCount">The updated Count.</param>
/// <param name="Errors">The errors.</param>
public sealed record VehicleImportResponse(int CreatedCount, int UpdatedCount, IReadOnlyList<VehicleImportError> Errors);
