using System.Text.Json.Serialization;

namespace de.openelp.feuerwehr.Common.Models.Inventory
{
    [JsonConverter(typeof(JsonStringEnumConverter<EquipmentStatus>))]
    public enum EquipmentStatus { Ready, Defective, InRepair }
}
