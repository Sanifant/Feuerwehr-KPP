using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory;

/// <summary>
/// Executes equipment Detail Dto.
/// </summary>
/// <param name="Equipment">The equipment.</param>
/// <param name="Inspections">The inspections.</param>
public sealed record EquipmentDetailDto(EquipmentDto Equipment, IReadOnlyList<EquipmentInspectionDto> Inspections);
