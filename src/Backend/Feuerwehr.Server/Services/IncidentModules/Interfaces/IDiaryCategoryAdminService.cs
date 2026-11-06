using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    public interface IDiaryCategoryAdminService
    {
        Task<ServiceResult> GetAll(CancellationToken cancellationToken);

        Task<ServiceResult> Upsert(UpsertDiaryCategoryRequest request, CancellationToken cancellationToken);
    }
}
