using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Inventory;

[JsonConverter(typeof(JsonStringEnumConverter<EquipmentStatus>))]
public enum EquipmentStatus { Ready, Defective, InRepair }
