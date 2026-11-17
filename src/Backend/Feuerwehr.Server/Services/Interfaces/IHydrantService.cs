using de.openelp.feuerwehr.Common.Models;

namespace de.openelp.feuerwehr.Server.Services
{
    public interface IHydrantService
    {
        ServiceResult Get();
        ServiceResult Get(int id);
        ServiceResult Post(HydrantDto value);
        ServiceResult Put(int id, HydrantDto value);
        ServiceResult Delete(int id);
        Task<byte[]> ExportAsync(CancellationToken cancellationToken = default);
        Task<ServiceResult> ImportAsync(Stream csv, CancellationToken cancellationToken = default);
    }
}
