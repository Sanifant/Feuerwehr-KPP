using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    public interface IDiaryCategoryAdminService
    {
        Task<ServiceResult> GetAll(CancellationToken cancellationToken);
        Task<ServiceResult> Upsert(UpsertDiaryCategoryRequest request, CancellationToken cancellationToken);
    }
}
