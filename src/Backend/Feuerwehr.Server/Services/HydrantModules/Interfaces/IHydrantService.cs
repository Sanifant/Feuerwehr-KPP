using de.openelp.feuerwehr.Common.Models;
using de.openelp.feuerwehr.Common.Models.HydrantInspections;

namespace de.openelp.feuerwehr.Server.Services.HydrantModules.Interfaces
{
    public interface IHydrantService
    {
        public ServiceResult Get();

        public ServiceResult Get(int id);

        public ServiceResult Post(HydrantDto value);

        public ServiceResult Put(int id, HydrantDto value);

        public ServiceResult Delete(int id);

        public ServiceResult Inspect(int hydrantId, CreateHydrantInspectionRequest request);

        public ServiceResult GetInspections(int hydrantId);

        /// <summary>
        ///
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public Task<byte[]> ExportAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="csv"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public Task<ServiceResult> ImportAsync(Stream csv, CancellationToken cancellationToken = default);
    }
}
