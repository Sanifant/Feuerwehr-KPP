using System.Globalization;
using System.Net;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IVehicleMailDispatcher
    {
        Task Dispatch(CancellationToken ct);
    }
}
