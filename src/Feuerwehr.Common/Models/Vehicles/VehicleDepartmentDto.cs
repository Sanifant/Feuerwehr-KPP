using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles;

/// <summary>
/// Executes vehicle Department Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
public sealed record VehicleDepartmentDto(int Id, string Name);
