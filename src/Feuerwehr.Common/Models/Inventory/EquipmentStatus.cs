using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory
{
    /// <summary>
    /// Defines the supported equipment status values.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<EquipmentStatus>))]
    public enum EquipmentStatus
    {
        /// <summary>
        /// The equipment is ready for operational use.
        /// </summary>
        Ready,

        /// <summary>
        /// The item has a reported defect.
        /// </summary>
        Defective,

        /// <summary>
        /// The equipment is being repaired.
        /// </summary>
        InRepair
    }
}
