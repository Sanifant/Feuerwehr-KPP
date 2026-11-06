using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    public interface IIncidentService
    {
        Task<ServiceResult> GetActiveIncident(CancellationToken cancellationToken);

        Task<ServiceResult> GetArchive(int page = 1, int pageSize = 25, CancellationToken cancellationToken = default);

        Task<ServiceResult> CreateIncident(CreateIncidentRequest request, CancellationToken cancellationToken);

        Task<ServiceResult> CloseIncident(Guid incidentId, CloseIncidentRequest request, CancellationToken cancellationToken);
    }
}
