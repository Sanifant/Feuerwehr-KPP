using De.Openelp.Feuerwehr.Server.Models.Vehicles;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IVehicleMailComposer
    {
        VehicleMailMessage Compose(VehicleReservation reservation, string recipient, bool request);
    }
}
