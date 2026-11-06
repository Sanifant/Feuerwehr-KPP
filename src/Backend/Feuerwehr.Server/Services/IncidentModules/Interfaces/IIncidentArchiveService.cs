using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    public interface IIncidentArchiveService
    {
        Task<ServiceResult> GetDiary(Guid incidentId, CancellationToken cancellationToken);
        Task<ServiceResult> ExportPdf(Guid incidentId, CancellationToken cancellationToken);
    }
}
