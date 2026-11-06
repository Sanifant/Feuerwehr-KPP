using System.Text.Json.Serialization;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Defines the supported vehicle reservation status values.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<VehicleReservationStatus>))]
    public enum VehicleReservationStatus
    {
        /// <summary>
        /// The reservation is awaiting an authorized approval or rejection.
        /// </summary>
        Pending,

        /// <summary>
        /// The reservation is confirmed and its calendar days are claimed.
        /// </summary>
        Confirmed,

        /// <summary>
        /// The reservation request has been rejected.
        /// </summary>
        Rejected,

        /// <summary>
        /// The reservation was canceled and no longer claims calendar days.
        /// </summary>
        Cancelled
    }
}
