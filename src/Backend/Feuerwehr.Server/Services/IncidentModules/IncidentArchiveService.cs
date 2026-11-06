using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Implements incident archive operations for the API application layer.
    /// </summary>
    /// <param name="dbContext">The database context for the current unit of work.</param>
    /// <param name="diaryService">The incident diary service dependency used by this component.</param>
    /// <param name="userContext">The accessor for the authenticated caller's identity and permissions.</param>
    public sealed class IncidentArchiveService(
        FeuerwehrDbContext dbContext,
        IIncidentDiaryService diaryService,
        IUserContextAccessor userContext) : IIncidentArchiveService
    {
        /// <summary>
        /// Retrieves the archived incident diary with its retained revisions.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the diary entries and revisions, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> GetDiary(Guid incidentId, CancellationToken cancellationToken)
        {
            if (!userContext.IsAdmin)
            {
                return ServiceResult.Forbid();
            }

            if (!await dbContext.Incidents.AnyAsync(x => x.Id == incidentId && x.Status == IncidentStatus.Closed, cancellationToken))
            {
                return ServiceResult.NotFound();
            }

            return await diaryService.GetEntries(incidentId, cancellationToken);
        }

        /// <summary>
        /// Exports the archived incident diary as a PDF document.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> ExportPdf(Guid incidentId, CancellationToken cancellationToken)
        {
            if (!userContext.IsAdmin)
            {
                return ServiceResult.Forbid();
            }

            if (!await dbContext.Incidents.AnyAsync(x => x.Id == incidentId && x.Status == IncidentStatus.Closed, cancellationToken))
            {
                return ServiceResult.NotFound();
            }

            return await diaryService.ExportDiaryPdf(incidentId, cancellationToken);
        }
    }
}
