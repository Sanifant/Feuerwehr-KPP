using System.Text.Json.Serialization;

namespace de.openelp.feuerwehr.Common.Models.Vehicles
{
    [JsonConverter(typeof(JsonStringEnumConverter<VehicleDayStatus>))]
    public enum VehicleDayStatus { Free, Pending, Booked, Unavailable }
}
