using de.openelp.feuerwehr.Server.Models.Vehicles;

namespace de.openelp.feuerwehr.Server.Services
{
    /// <summary>Creates vehicle notifications within the reservation's unit of work.</summary>
    public interface IVehicleMailService
    {
        /// <summary>Stages a reservation request or confirmation for central delivery.</summary>
        /// <param name="reservation">The reservation to describe.</param>
        /// <param name="recipient">The recipient email address.</param>
        /// <param name="request">Whether this is a request rather than a confirmation.</param>
        /// <param name="createdBy">The user performing the reservation action.</param>
        void QueueMail(VehicleReservation reservation, string recipient, bool request, string createdBy);
    }
}
