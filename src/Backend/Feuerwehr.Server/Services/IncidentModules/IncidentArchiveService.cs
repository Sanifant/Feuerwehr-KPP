using de.openelp.feuerwehr.Common.Models.IncidentModules;
using de.openelp.feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace de.openelp.feuerwehr.Server.Services.IncidentModules;

public interface IIncidentArchiveService
{
    Task<ServiceResult> GetDiary(Guid incidentId, CancellationToken cancellationToken);
    Task<ServiceResult> ExportPdf(Guid incidentId, CancellationToken cancellationToken);
}

public sealed class IncidentArchiveService(FeuerwehrDbContext dbContext, IIncidentDiaryService diaryService,
    IUserContextAccessor userContext) : IIncidentArchiveService
{
    public async Task<ServiceResult> GetDiary(Guid incidentId, CancellationToken cancellationToken)
    {
        if (!userContext.IsAdmin) return ServiceResult.Forbid();
        if (!await dbContext.Incidents.AnyAsync(x => x.Id == incidentId && x.Status == IncidentStatus.Closed, cancellationToken))
            return ServiceResult.NotFound();
        return await diaryService.GetEntries(incidentId, cancellationToken);
    }

    public async Task<ServiceResult> ExportPdf(Guid incidentId, CancellationToken cancellationToken)
    {
        if (!userContext.IsAdmin) return ServiceResult.Forbid();
        if (!await dbContext.Incidents.AnyAsync(x => x.Id == incidentId && x.Status == IncidentStatus.Closed, cancellationToken))
            return ServiceResult.NotFound();
        return await diaryService.ExportDiaryPdf(incidentId, cancellationToken);
    }
}
