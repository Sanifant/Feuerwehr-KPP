using System.Globalization;
using System.Text;
using De.Openelp.Feuerwehr.Common.Models;
using De.Openelp.Feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore;
using AddressEntity = De.Openelp.Feuerwehr.Server.Models.Address;
using HydrantEntity = De.Openelp.Feuerwehr.Server.Models.Hydrant;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IHydrantService
    {
        ServiceResult Get();
        ServiceResult Get(int id);
        ServiceResult Post(Hydrant value);
        ServiceResult Put(int id, Hydrant value);
        ServiceResult Delete(int id);
        Task<byte[]> ExportAsync(CancellationToken cancellationToken = default);
        Task<ServiceResult> ImportAsync(Stream csv, CancellationToken cancellationToken = default);
    }
}
