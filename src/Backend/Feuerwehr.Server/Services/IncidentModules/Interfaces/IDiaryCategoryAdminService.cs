using de.openelp.feuerwehr.Common.Models.IncidentModules;

namespace de.openelp.feuerwehr.Server.Services.IncidentModules
{
    public interface IDiaryCategoryAdminService
    {
        Task<ServiceResult> GetAll(CancellationToken cancellationToken);
        Task<ServiceResult> Upsert(UpsertDiaryCategoryRequest request, CancellationToken cancellationToken);
    }
}
