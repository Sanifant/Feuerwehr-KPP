namespace de.openelp.feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Executes equipment Detail Dto.
    /// </summary>
    /// <param name="Equipment">The equipment.</param>
    /// <param name="Inspections">The inspections.</param>
    public sealed record EquipmentDetailDto(EquipmentDto Equipment, IReadOnlyList<EquipmentInspectionDto> Inspections);
}
