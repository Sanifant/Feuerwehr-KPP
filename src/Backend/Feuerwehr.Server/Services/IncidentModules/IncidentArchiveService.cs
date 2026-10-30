using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules;

public interface IIncidentArchiveService
{
    Task<ServiceResult> GetDiary(Guid incidentId, CancellationToken cancellationToken);
    Task<ServiceResult> ExportPdf(Guid incidentId, CancellationToken cancellationToken);
}

/// <summary>
/// Executes incident Archive Service.
/// </summary>
/// <param name="dbContext">The db Context.</param>
/// <param name="diaryService">The diary Service.</param>
/// <param name="userContext">The user Context.</param>
public sealed class IncidentArchiveService(FeuerwehrDbContext dbContext, IIncidentDiaryService diaryService,
    IUserContextAccessor userContext) : IIncidentArchiveService
{
    /// <summary>
    /// Gets the diary.
    /// </summary>
    /// <param name="incidentId">The incident Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<ServiceResult> GetDiary(Guid incidentId, CancellationToken cancellationToken)
    {
        if (!userContext.IsAdmin) return ServiceResult.Forbid();
        if (!await dbContext.Incidents.AnyAsync(x => x.Id == incidentId && x.Status == IncidentStatus.Closed, cancellationToken))
            return ServiceResult.NotFound();
        return await diaryService.GetEntries(incidentId, cancellationToken);
    }

    /// <summary>
    /// Executes export Pdf.
    /// </summary>
    /// <param name="incidentId">The incident Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<ServiceResult> ExportPdf(Guid incidentId, CancellationToken cancellationToken)
    {
        if (!userContext.IsAdmin) return ServiceResult.Forbid();
        if (!await dbContext.Incidents.AnyAsync(x => x.Id == incidentId && x.Status == IncidentStatus.Closed, cancellationToken))
            return ServiceResult.NotFound();
        return await diaryService.ExportDiaryPdf(incidentId, cancellationToken);
    }
}

