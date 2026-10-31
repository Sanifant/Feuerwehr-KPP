using System.Globalization;
using System.Net;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IVehicleMailComposer
    {
        VehicleMailMessage Compose(VehicleReservation reservation, string recipient, bool request);
    }
}
