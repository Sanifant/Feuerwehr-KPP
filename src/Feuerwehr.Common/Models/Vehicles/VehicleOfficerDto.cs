using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles;

/// <summary>
/// Executes vehicle Officer Dto.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
/// <param name="FireDepartmentId">The fire Department Id.</param>
public sealed record VehicleOfficerDto(string Id, string Name, int FireDepartmentId);
