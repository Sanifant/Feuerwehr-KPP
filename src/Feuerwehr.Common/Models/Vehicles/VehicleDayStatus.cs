using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles;

[JsonConverter(typeof(JsonStringEnumConverter<VehicleDayStatus>))]
public enum VehicleDayStatus { Free, Pending, Booked, Unavailable }
