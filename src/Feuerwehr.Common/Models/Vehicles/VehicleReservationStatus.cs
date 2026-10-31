using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    [JsonConverter(typeof(JsonStringEnumConverter<VehicleReservationStatus>))]
    public enum VehicleReservationStatus { Pending, Confirmed, Rejected, Cancelled }
}
