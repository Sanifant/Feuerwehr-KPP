using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles;

/// <summary>
/// Executes vehicle Management Options Dto.
/// </summary>
/// <param name="Departments">The departments.</param>
/// <param name="Officers">The officers.</param>
/// <param name="DefaultStatusId">The default Status Id.</param>
public sealed record VehicleManagementOptionsDto(IReadOnlyList<VehicleDepartmentDto> Departments, IReadOnlyList<VehicleOfficerDto> Officers, Guid DefaultStatusId = default);
