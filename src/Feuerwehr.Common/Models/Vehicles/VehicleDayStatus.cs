using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Defines the supported vehicle day status values.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<VehicleDayStatus>))]
    public enum VehicleDayStatus
    {
        /// <summary>
        /// The vehicle is available on this calendar day.
        /// </summary>
        Free,

        /// <summary>
        /// The calendar day has a pending reservation request but is not yet confirmed.
        /// </summary>
        Pending,

        /// <summary>
        /// The calendar day is claimed by a confirmed vehicle booking.
        /// </summary>
        Booked,

        /// <summary>
        /// The vehicle cannot be booked on this calendar day.
        /// </summary>
        Unavailable
    }
}
