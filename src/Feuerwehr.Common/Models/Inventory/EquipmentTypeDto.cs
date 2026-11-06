using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Executes equipment Type Dto.
    /// </summary>
    /// <param name="Id">The id.</param>
    /// <param name="CategoryId">The category Id.</param>
    /// <param name="CategoryName">The category Name.</param>
    /// <param name="Name">The name.</param>
    /// <param name="InspectionIntervalMonths">The inspection Interval Months.</param>
    /// <param name="InspectionInstructions">The inspection Instructions.</param>
    /// <param name="Version">The version.</param>
    public sealed record EquipmentTypeDto(Guid Id, Guid CategoryId, string CategoryName, string Name,
        int InspectionIntervalMonths, string InspectionInstructions, int Version);
}
