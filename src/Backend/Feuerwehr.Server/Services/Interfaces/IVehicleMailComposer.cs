using De.Openelp.Feuerwehr.Server.Models.Vehicles;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the vehicle mail composer operations required by application consumers.
    /// </summary>
    public interface IVehicleMailComposer
    {
        /// <summary>
        /// Creates the plain-text and HTML notification for a reservation request or confirmed booking.
        /// </summary>
        /// <param name="reservation">The reservation whose captured booking details are included in the notification.</param>
        /// <param name="recipient">The destination email address.</param>
        /// <param name="request">Whether to compose a pending-request notification rather than a booking confirmation.</param>
        /// <returns>The queued notification content and initial delivery metadata.</returns>
        VehicleMailMessage Compose(VehicleReservation reservation, string recipient, bool request);
    }
}
