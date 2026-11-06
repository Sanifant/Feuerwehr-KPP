using de.openelp.feuerwehr.Server.Models.Vehicles;

namespace de.openelp.feuerwehr.Server.Services
{
    public interface IVehicleMailComposer
    {
        VehicleMailMessage Compose(VehicleReservation reservation, string recipient, bool request);
    }
}
