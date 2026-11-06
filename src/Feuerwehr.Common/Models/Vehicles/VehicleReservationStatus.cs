using System.Text.Json.Serialization;

namespace de.openelp.feuerwehr.Common.Models.Vehicles
{
    [JsonConverter(typeof(JsonStringEnumConverter<VehicleReservationStatus>))]
    public enum VehicleReservationStatus { Pending, Confirmed, Rejected, Cancelled }
}
