namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Carries equipment detail data across the API boundary.
    /// </summary>
    /// <param name="Equipment">The equipment record associated with this equipment detail.</param>
    /// <param name="Inspections">The inspections associated with this equipment detail.</param>
    public sealed record EquipmentDetailDto(EquipmentDto Equipment, IReadOnlyList<EquipmentInspectionDto> Inspections);
}
