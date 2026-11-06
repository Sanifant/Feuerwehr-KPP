using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    public interface IIncidentDiaryService
    {
        Task<ServiceResult> GetEntries(Guid incidentId, CancellationToken cancellationToken);

        Task<ServiceResult> GetCategories(CancellationToken cancellationToken);

        Task<ServiceResult> CreateEntry(Guid incidentId, CreateDiaryEntryRequest request, CancellationToken cancellationToken);

        Task<ServiceResult> UpdateEntry(Guid incidentId, Guid entryId, UpdateDiaryEntryRequest request, CancellationToken cancellationToken);

        Task<ServiceResult> CancelEntry(Guid incidentId, Guid entryId, CancelDiaryEntryRequest request, CancellationToken cancellationToken);

        Task<ServiceResult> ExportDiaryPdf(Guid incidentId, CancellationToken cancellationToken);
    }
}
